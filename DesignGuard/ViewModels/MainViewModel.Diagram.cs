// Diagram layout, drag en zoom.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;
using DesignGuard.Services;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private void SelectComponentFromDiagram(int componentId)
    {
        var row = Components.FirstOrDefault(c => c.Id == componentId);
        if (row != null)
            SelectedComponent = row;
    }

    private void RefreshDiagram()
    {
        try
        {
            var m = BuildModelFromEditor();
            var layout = _diagramLayout.Layout(m);
            // Geen * DiagramZoom: schaal alleen via LayoutTransform op de grid (anders dubbele zoom).
            DiagramContentWidth = Math.Max(400, layout.ContentWidth);
            DiagramContentHeight = Math.Max(300, layout.ContentHeight);
            var threatForLink = NavSection == 1 && DiagramOverlayThreatLinks ? SelectedThreat : null;

            DiagramNodes = new ObservableCollection<DiagramNodeViewModel>(layout.Nodes.Select(n =>
            {
                var showSen = DiagramOverlaySensitiveData &&
                              DesignOntwerpWaarden.IsDataSensitivityVisuallyElevated(n.StoresOrProcessesLabel);
                var linked = threatForLink != null &&
                             threatForLink.AffectedComponents.Exists(a =>
                                 string.Equals(a, n.Name, StringComparison.OrdinalIgnoreCase));
                return new DiagramNodeViewModel
                {
                    ComponentId = n.ComponentId,
                    Name = n.Name,
                    Tag = n.Tag,
                    DataSensitivity = n.StoresOrProcessesLabel,
                    X = n.X,
                    Y = n.Y,
                    IsEntryPoint = n.IsEntryPoint,
                    IsHighlighted = SelectedComponent?.Id == n.ComponentId,
                    ShowSensitiveStripe = showSen,
                    IsLinkedHighlight = linked
                };
            }));
            DiagramLines = new ObservableCollection<DiagramLineViewModel>(layout.Edges.Select(e =>
                new DiagramLineViewModel
                {
                    FromId = e.FromId,
                    ToId = e.ToId,
                    CurvePath = e.CurvePath,
                    ArrowPath = e.ArrowPath,
                    LabelDrawLeft = e.LabelDrawLeft,
                    LabelDrawTop = e.LabelDrawTop,
                    Label = e.Label,
                    LateralStart = e.LateralStart,
                    LateralEnd = e.LateralEnd,
                    LabelT = e.LabelT
                }));
            DiagramTrustOverlays = new ObservableCollection<TrustBoundaryOverlayViewModel>(
                layout.TrustOverlays.Select(o => new TrustBoundaryOverlayViewModel
                {
                    X = o.X,
                    Y = o.Y,
                    Width = o.Width,
                    Height = o.Height,
                    Name = o.Name,
                    Color = o.ColorHint,
                    IsVisible = DiagramOverlayTrustBoundaries
                }));
        }
        catch
        {
            // layout mag editor niet breken
        }
    }

    /// <summary>Live-update tijdens het slepen: node verplaatsen en bogen naar/van deze node opnieuw tekenen.</summary>
    public void UpdateDraggedNodePosition(int componentId, double x, double y)
    {
        var node = DiagramNodes.FirstOrDefault(n => n.ComponentId == componentId);
        if (node == null) return;
        // Clamp op niet-negatief zodat bogen niet achter de canvas randen verdwijnen
        node.X = Math.Max(0, x);
        node.Y = Math.Max(0, y);

        // Bogen die deze node raken opnieuw berekenen met gecachete fan-offsets
        foreach (var line in DiagramLines)
        {
            if (line.FromId != componentId && line.ToId != componentId) continue;
            var from = DiagramNodes.FirstOrDefault(n => n.ComponentId == line.FromId);
            var to = DiagramNodes.FirstOrDefault(n => n.ComponentId == line.ToId);
            if (from == null || to == null) continue;
            var (curve, arrow, lcx, lcy) = DiagramEdgeGeometry.Build(
                from.X, from.Y, to.X, to.Y, line.Label, line.LateralStart, line.LateralEnd, line.LabelT);
            line.CurvePath = curve;
            line.ArrowPath = arrow;
            // Label: halve breedte/hoogte-schatting zelfde als in layout service
            var halfW = Math.Clamp((line.Label.Length) * 3.4 + 14, 36, 110);
            var h = line.Label.Length > 34 ? 14 * 2 + 10 : 14 + 8;
            line.LabelDrawLeft = lcx - halfW;
            line.LabelDrawTop = lcy - h / 2;
        }

        // DiagramTrustOverlays meelopen (rechthoek om groep nodes heen)
        UpdateTrustOverlaysAfterDrag();
    }

    /// <summary>Einde drag: positie vastleggen op ComponentRow zodat die bewaard blijft en opnieuw wordt geladen.</summary>
    public void CommitDraggedNodePosition(int componentId, double x, double y)
    {
        var row = Components.FirstOrDefault(c => c.Id == componentId);
        if (row == null) return;
        row.VisualX = Math.Max(0, x);
        row.VisualY = Math.Max(0, y);
        // Volledige refresh: labels en fan-offsets opnieuw optimaliseren met nieuwe positie
        RefreshDiagram();
    }

    /// <summary>Reset alle handmatige posities en laat de auto-layout alles opnieuw neerzetten.</summary>
    [RelayCommand]
    private void ResetDiagramPositions()
    {
        foreach (var c in Components)
        {
            c.VisualX = null;
            c.VisualY = null;
        }
        RefreshDiagram();
    }

    private void UpdateTrustOverlaysAfterDrag()
    {
        // Trust boundary rechthoeken her-opspannen op basis van huidige node-posities
        const double pad = 22;
        const double nw = 196;
        const double nh = 64;
        foreach (var overlay in DiagramTrustOverlays)
        {
            var tbRow = TrustBoundaries.FirstOrDefault(t =>
                string.Equals(t.Name, overlay.Name, StringComparison.Ordinal));
            if (tbRow == null) continue;
            var members = Components.Where(c => c.TrustBoundaryId == tbRow.Id).Select(c => c.Id).ToHashSet();
            var nodesInTb = DiagramNodes.Where(n => members.Contains(n.ComponentId)).ToList();
            if (nodesInTb.Count == 0) continue;
            var minX = nodesInTb.Min(n => n.X) - pad;
            var minY = nodesInTb.Min(n => n.Y) - pad;
            var maxX = nodesInTb.Max(n => n.X) + nw + pad;
            var maxY = nodesInTb.Max(n => n.Y) + nh + pad;
            overlay.X = minX;
            overlay.Y = minY;
            overlay.Width = maxX - minX;
            overlay.Height = maxY - minY;
        }
    }

    [RelayCommand]
    private void DiagramZoomIn()
    {
        DiagramZoom = Math.Min(2.2, Math.Round(DiagramZoom + 0.1, 2));
    }

    [RelayCommand]
    private void DiagramZoomOut()
    {
        DiagramZoom = Math.Max(0.5, Math.Round(DiagramZoom - 0.1, 2));
    }

    [RelayCommand]
    private void DiagramFitToScreen()
    {
        DiagramZoom = 1.0;
    }

    [RelayCommand]
    private void RefreshDiagramLayout() => RefreshDiagram();

    partial void OnSelectedComponentChanged(ComponentRowViewModel? value)
    {
        foreach (var n in DiagramNodes)
            n.IsHighlighted = value != null && n.ComponentId == value.Id;
    }
}

// Diagram layout en zoom.
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
            DiagramContentWidth = Math.Max(400, layout.ContentWidth * DiagramZoom);
            DiagramContentHeight = Math.Max(300, layout.ContentHeight * DiagramZoom);
            var threat = NavSection == 1 && DiagramOverlayThreatLinks ? SelectedThreat : null;
            DiagramNodes = new ObservableCollection<DiagramNodeViewModel>(layout.Nodes.Select(n =>
            {
                var showSen = DiagramOverlaySensitiveData && n.DataSensitivity != DataSensitivity.None;
                var linked = threat != null &&
                             threat.AffectedComponents.Exists(a =>
                                 string.Equals(a, n.Name, StringComparison.OrdinalIgnoreCase));
                return new DiagramNodeViewModel
                {
                    ComponentId = n.ComponentId,
                    Name = n.Name,
                    Tag = n.Tag,
                    DataSensitivity = n.DataSensitivity.ToString(),
                    X = n.X * DiagramZoom,
                    Y = n.Y * DiagramZoom,
                    IsEntryPoint = n.IsEntryPoint,
                    IsHighlighted = SelectedComponent?.Id == n.ComponentId,
                    ShowSensitiveStripe = showSen,
                    IsLinkedHighlight = linked
                };
            }));
            var lines = layout.Edges.Select(e =>
            {
                var from = layout.Nodes.FirstOrDefault(x => x.ComponentId == e.FromId);
                var to = layout.Nodes.FirstOrDefault(x => x.ComponentId == e.ToId);
                if (from == null || to == null) return null;
                var (path, lx, ly) = DiagramEdgeGeometry.Build(
                    from.X * DiagramZoom,
                    from.Y * DiagramZoom,
                    to.X * DiagramZoom,
                    to.Y * DiagramZoom,
                    e.Label);
                return new DiagramLineViewModel
                {
                    PathData = path,
                    LabelX = lx,
                    LabelY = ly,
                    Label = e.Label
                };
            }).Where(x => x != null).Cast<DiagramLineViewModel>().ToList();
            DiagramLines = new ObservableCollection<DiagramLineViewModel>(lines);
            DiagramTrustOverlays = new ObservableCollection<TrustBoundaryOverlayViewModel>(
                layout.TrustOverlays.Select(o => new TrustBoundaryOverlayViewModel
                {
                    X = o.X * DiagramZoom,
                    Y = o.Y * DiagramZoom,
                    Width = o.Width * DiagramZoom,
                    Height = o.Height * DiagramZoom,
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

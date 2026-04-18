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
            // Geen * DiagramZoom: schaal alleen via LayoutTransform op de grid (anders dubbele zoom).
            DiagramContentWidth = Math.Max(400, layout.ContentWidth);
            DiagramContentHeight = Math.Max(300, layout.ContentHeight);
            var threat = NavSection == 1 && DiagramOverlayThreatLinks ? SelectedThreat : null;
            DiagramNodes = new ObservableCollection<DiagramNodeViewModel>(layout.Nodes.Select(n =>
            {
                var showSen = DiagramOverlaySensitiveData &&
                              DesignOntwerpWaarden.IsDataSensitivityVisuallyElevated(n.StoresOrProcessesLabel);
                var linked = threat != null &&
                             threat.AffectedComponents.Exists(a =>
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
                    CurvePath = e.CurvePath,
                    ArrowPath = e.ArrowPath,
                    LabelX = e.LabelX,
                    LabelY = e.LabelY,
                    Label = e.Label
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

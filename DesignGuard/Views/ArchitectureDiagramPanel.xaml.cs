using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class ArchitectureDiagramPanel : UserControl
{
    public ArchitectureDiagramPanel() => InitializeComponent();

    public bool ShowThreatLinkCheckbox
    {
        get => (bool)GetValue(ShowThreatLinkCheckboxProperty);
        set => SetValue(ShowThreatLinkCheckboxProperty, value);
    }

    public static readonly DependencyProperty ShowThreatLinkCheckboxProperty =
        DependencyProperty.Register(nameof(ShowThreatLinkCheckbox), typeof(bool), typeof(ArchitectureDiagramPanel),
            new PropertyMetadata(true));

    // Actieve drag-state: welk Border wordt gesleept, onder welke muispositie startte het, en de ID
    private Border? _draggedBorder;
    private int _draggedComponentId;
    private Point _dragOffsetInNode;
    private bool _didMove;

    private void DiagramNode_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not Border b || b.Tag is not int cid)
            return;
        vm.SelectComponentFromDiagramCommand.Execute(cid);
        // Offset tussen cursor en linkerbovenhoek van de node onthouden: de node blijft
        // onder dezelfde plek van de cursor plakken, anders "springt" hij bij start drag.
        _dragOffsetInNode = e.GetPosition(b);
        _draggedBorder = b;
        _draggedComponentId = cid;
        _didMove = false;
        b.CaptureMouse();
        e.Handled = true;
    }

    private void DiagramNode_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedBorder == null || e.LeftButton != MouseButtonState.Pressed) return;
        if (DataContext is not MainViewModel vm) return;
        // Cursor-positie omrekenen naar canvas-coördinaten van DiagramGrid (pre-zoom)
        var posInGrid = e.GetPosition(DiagramGrid);
        var newX = posInGrid.X - _dragOffsetInNode.X;
        var newY = posInGrid.Y - _dragOffsetInNode.Y;
        vm.UpdateDraggedNodePosition(_draggedComponentId, newX, newY);
        _didMove = true;
    }

    private void DiagramNode_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedBorder == null) return;
        // Alleen committen bij echt slepen; een simpele click op een node telt als selectie
        if (_didMove && DataContext is MainViewModel vm &&
            _draggedBorder.DataContext is DiagramNodeViewModel nvm)
        {
            vm.CommitDraggedNodePosition(_draggedComponentId, nvm.X, nvm.Y);
        }
        _draggedBorder.ReleaseMouseCapture();
        _draggedBorder = null;
        _didMove = false;
        e.Handled = true;
    }

    private void DiagramNode_LostCapture(object sender, MouseEventArgs e)
    {
        // Fallback als capture wordt afgenomen (andere dialoog, focus-verlies): drag-state opruimen
        _draggedBorder = null;
        _didMove = false;
    }
}

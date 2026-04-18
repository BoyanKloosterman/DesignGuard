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

    private void DiagramNode_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement fe || fe.Tag is not int cid)
            return;
        vm.SelectComponentFromDiagramCommand.Execute(cid);
    }
}

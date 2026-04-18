using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainDesignView : UserControl
{
    public MainDesignView() => InitializeComponent();

    private void DiagramNode_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement fe || fe.Tag is not int cid)
            return;
        vm.SelectComponentFromDiagramCommand.Execute(cid);
    }
}

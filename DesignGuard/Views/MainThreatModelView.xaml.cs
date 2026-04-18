using System.Windows.Controls;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainThreatModelView
{
    public MainThreatModelView() => InitializeComponent();

    private void C4Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.RefreshC4AfterGridEdit();
    }
}

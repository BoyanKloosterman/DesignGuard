using System.Windows.Controls;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainC4View
{
    public MainC4View() => InitializeComponent();

    private void C4Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.RefreshC4AfterGridEdit();
    }
}

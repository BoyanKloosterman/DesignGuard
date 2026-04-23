using System.Windows.Controls;
using System.Windows.Threading;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainC4View
{
    private DispatcherTimer? _c4ParentChangeDebounce;
    private MainViewModel? _c4ParentDebounceVm;

    public MainC4View() => InitializeComponent();

    private void C4Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.RefreshC4AfterGridEdit();
    }

    // CellTemplate ComboBox triggert geen CellEditEnding; debounce tegen load-storm per rij.
    private void C4ParentCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || e.AddedItems.Count == 0 || DataContext is not MainViewModel vm) return;
        _c4ParentDebounceVm = vm;
        _c4ParentChangeDebounce ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _c4ParentChangeDebounce.Stop();
        _c4ParentChangeDebounce.Tick -= OnC4ParentDebounce;
        _c4ParentChangeDebounce.Tick += OnC4ParentDebounce;
        _c4ParentChangeDebounce.Start();
    }

    private void OnC4ParentDebounce(object? sender, EventArgs e)
    {
        if (_c4ParentChangeDebounce == null) return;
        _c4ParentChangeDebounce.Stop();
        _c4ParentChangeDebounce.Tick -= OnC4ParentDebounce;
        _c4ParentDebounceVm?.RefreshC4AfterGridEdit();
    }
}

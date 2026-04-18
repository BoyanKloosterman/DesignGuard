using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DesignGuard.Models;
using DesignGuard.ViewModels;

namespace DesignGuard;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.InitializeCommand.ExecuteAsync(null);
            ThreatSortBox.SelectedIndex = 0;
            ReqSortBox.SelectedIndex = 0;
        }
    }

    private void DiagramNode_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement fe || fe.Tag is not int cid)
            return;
        vm.SelectComponentFromDiagramCommand.Execute(cid);
    }

    private void ThreatSortBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not ComboBox cb || cb.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag)
            return;
        vm.ThreatSort = tag;
    }

    private void ReqSortBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not ComboBox cb || cb.SelectedItem is not ComboBoxItem item ||
            item.Tag is not string tag)
            return;
        vm.RequirementSort = tag;
    }

    private void ControlAddRequirement_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox cb || cb.SelectedItem is not RequirementModel req) return;
        if (FindAncestor<DataGridRow>(cb) is not { DataContext: ControlRowViewModel row }) return;
        row.AddLinkedRequirement(req);
        cb.SelectedItem = null;
    }

    private static T? FindAncestor<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T match) return match;
            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}

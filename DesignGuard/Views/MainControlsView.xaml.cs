using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesignGuard.Models;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainControlsView : UserControl
{
    public MainControlsView() => InitializeComponent();

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

using System.Windows;
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
            await vm.InitializeCommand.ExecuteAsync(null);
    }
}

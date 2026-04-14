using System.Windows;
using DesignGuard.Models;
using DesignGuard.ViewModels;

namespace DesignGuard;

public partial class ProjectWizardWindow : Window
{
    private readonly MainViewModel _vm;

    public ProjectWizardWindow(MainViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
        SysType.ItemsSource = Enum.GetNames(typeof(SystemType));
        DeployCtx.ItemsSource = Enum.GetNames(typeof(DeploymentContext));
        ProjName.Text = vm.EditorProjectName;
        ProjDesc.Text = vm.EditorProjectDescription;
        SysName.Text = vm.EditorSystemName;
        SysType.SelectedItem = vm.EditorSystemType;
        DeployCtx.SelectedItem = vm.EditorDeploymentContext;
        ChkInternet.IsChecked = vm.FlagInternetExposed;
        ChkLog.IsChecked = vm.FlagLoggingMonitoring;
        ChkCritical.IsChecked = vm.FlagCriticalBusiness;
        ChkPersonal.IsChecked = vm.FlagPersonalData;
        ChkAuth.IsChecked = vm.FlagAuth;
        ChkAdmin.IsChecked = vm.FlagAdmin;
        ChkExt.IsChecked = vm.FlagExternalApi;
        ChkUpload.IsChecked = vm.FlagUpload;
        ChkSens.IsChecked = vm.FlagSensitiveStorage;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _vm.EditorProjectName = ProjName.Text.Trim();
        _vm.EditorProjectDescription = ProjDesc.Text.Trim();
        _vm.EditorSystemName = SysName.Text.Trim();
        if (SysType.SelectedItem is string st)
            _vm.EditorSystemType = st;
        if (DeployCtx.SelectedItem is string dc)
            _vm.EditorDeploymentContext = dc;
        _vm.FlagInternetExposed = ChkInternet.IsChecked == true;
        _vm.FlagLoggingMonitoring = ChkLog.IsChecked == true;
        _vm.FlagCriticalBusiness = ChkCritical.IsChecked == true;
        _vm.FlagPersonalData = ChkPersonal.IsChecked == true;
        _vm.FlagAuth = ChkAuth.IsChecked == true;
        _vm.FlagAdmin = ChkAdmin.IsChecked == true;
        _vm.FlagExternalApi = ChkExt.IsChecked == true;
        _vm.FlagUpload = ChkUpload.IsChecked == true;
        _vm.FlagSensitiveStorage = ChkSens.IsChecked == true;

        var roles = RolesQuick.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (roles.Length > 0)
        {
            _vm.Roles.Clear();
            foreach (var r in roles)
                _vm.Roles.Add(new RoleRowViewModel { Name = r, Description = "" });
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

using System.Windows;
using System.Windows.Controls;
using DesignGuard.Models;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainDetailsInspectorView : UserControl
{
    private string? _threatStatusAuditCaptureId;
    private ThreatStatus _threatStatusAuditCaptureValue;
    private string? _requirementStatusAuditCaptureId;
    private RequirementStatus _requirementStatusAuditCaptureValue;

    public MainDetailsInspectorView() => InitializeComponent();

    private void CaptureThreatStatusBaseline(ComboBox? cb)
    {
        if (cb?.DataContext is ThreatModel t)
        {
            _threatStatusAuditCaptureId = t.Id;
            _threatStatusAuditCaptureValue = t.Status;
        }
    }

    private void ThreatRiskCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count == 0) return;
        if (DataContext is not MainViewModel vm) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not ThreatModel t) return;
        vm.OnThreatRiskChanged(t);
    }

    private void FindingRiskCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (e.RemovedItems.Count == 0) return;
        if (DataContext is not MainViewModel vm) return;
        if (sender is not FrameworkElement fe || fe.DataContext is not PentestFindingModel f) return;
        vm.OnFindingRiskChanged(f);
    }

    private void ThreatStatusCombo_GotFocus(object sender, RoutedEventArgs e) =>
        CaptureThreatStatusBaseline(sender as ComboBox);

    private void ThreatStatusCombo_DropDownOpened(object sender, EventArgs e) =>
        CaptureThreatStatusBaseline(sender as ComboBox);

    private void TryCommitThreatStatusAudit(ComboBox? cb)
    {
        if (DataContext is not MainViewModel vm) return;
        if (cb?.DataContext is not ThreatModel t) return;
        if (_threatStatusAuditCaptureId != t.Id) return;
        if (t.Status != _threatStatusAuditCaptureValue)
        {
            vm.ApplyThreatStatusAudit(t, _threatStatusAuditCaptureValue);
            _threatStatusAuditCaptureValue = t.Status;
        }
    }

    private void ThreatStatusCombo_LostFocus(object sender, RoutedEventArgs e) =>
        TryCommitThreatStatusAudit(sender as ComboBox);

    private void ThreatStatusCombo_DropDownClosed(object sender, EventArgs e) =>
        TryCommitThreatStatusAudit(sender as ComboBox);

    private void CaptureRequirementStatusBaseline(ComboBox? cb)
    {
        if (cb?.DataContext is RequirementModel r)
        {
            _requirementStatusAuditCaptureId = r.Id;
            _requirementStatusAuditCaptureValue = r.Status;
        }
    }

    private void RequirementStatusCombo_GotFocus(object sender, RoutedEventArgs e) =>
        CaptureRequirementStatusBaseline(sender as ComboBox);

    private void RequirementStatusCombo_DropDownOpened(object sender, EventArgs e) =>
        CaptureRequirementStatusBaseline(sender as ComboBox);

    private void TryCommitRequirementStatusAudit(ComboBox? cb)
    {
        if (DataContext is not MainViewModel vm) return;
        if (cb?.DataContext is not RequirementModel r) return;
        if (_requirementStatusAuditCaptureId != r.Id) return;
        if (r.Status != _requirementStatusAuditCaptureValue)
        {
            vm.ApplyRequirementStatusAudit(r, _requirementStatusAuditCaptureValue);
            _requirementStatusAuditCaptureValue = r.Status;
        }
    }

    private void RequirementStatusCombo_LostFocus(object sender, RoutedEventArgs e) =>
        TryCommitRequirementStatusAudit(sender as ComboBox);

    private void RequirementStatusCombo_DropDownClosed(object sender, EventArgs e) =>
        TryCommitRequirementStatusAudit(sender as ComboBox);
}

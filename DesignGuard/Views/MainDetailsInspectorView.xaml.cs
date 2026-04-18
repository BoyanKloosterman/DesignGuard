using System.Windows;
using System.Windows.Controls;
using DesignGuard.Models;
using DesignGuard.ViewModels;

namespace DesignGuard.Views;

public partial class MainDetailsInspectorView : UserControl
{
    private string? _threatStatusAuditCaptureId;
    private ThreatStatus _threatStatusAuditCaptureValue;

    public MainDetailsInspectorView() => InitializeComponent();

    private void CaptureThreatStatusBaseline(ComboBox? cb)
    {
        if (cb?.DataContext is ThreatModel t)
        {
            _threatStatusAuditCaptureId = t.Id;
            _threatStatusAuditCaptureValue = t.Status;
        }
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
}

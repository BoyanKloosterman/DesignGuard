// Audit bij wijziging dreiging-status (wie/wanneer).
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    /// <summary>Vastleggen na statuswijziging in UI (baseline = vorige status).</summary>
    public void ApplyThreatStatusAudit(ThreatModel threat, ThreatStatus previousStatus)
    {
        if (threat.Status == previousStatus) return;

        threat.StatusChangedAtUtc = DateTime.UtcNow;
        var by = (ReviewerDisplayName ?? "").Trim();
        if (string.IsNullOrEmpty(by))
            by = Environment.UserName;
        threat.StatusChangedBy = by;
        threat.UserModified = true;

        var sel = SelectedThreat;
        if (ReferenceEquals(sel, threat))
        {
            SelectedThreat = null;
            SelectedThreat = sel;
        }

        RefreshFilters();
        UpdateDashboard();
    }
}

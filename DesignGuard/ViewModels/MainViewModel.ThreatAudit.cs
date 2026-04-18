// Audit bij wijziging dreiging-/eis-status (wie/wanneer).
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    /// <summary>Naam voor audit: instelling reviewer, anders Windows-gebruiker.</summary>
    private string ResolveReviewerNameForAudit()
    {
        var by = (ReviewerDisplayName ?? "").Trim();
        return string.IsNullOrEmpty(by) ? Environment.UserName : by;
    }

    /// <summary>Vastleggen na statuswijziging in UI (baseline = vorige status).</summary>
    public void ApplyThreatStatusAudit(ThreatModel threat, ThreatStatus previousStatus)
    {
        if (threat.Status == previousStatus) return;

        threat.StatusChangedAtUtc = DateTime.UtcNow;
        threat.StatusChangedBy = ResolveReviewerNameForAudit();
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

    /// <summary>Zelfde audit als dreigingen, voor eisen-tab.</summary>
    public void ApplyRequirementStatusAudit(RequirementModel requirement, RequirementStatus previousStatus)
    {
        if (requirement.Status == previousStatus) return;

        requirement.StatusChangedAtUtc = DateTime.UtcNow;
        requirement.StatusChangedBy = ResolveReviewerNameForAudit();
        requirement.UserModified = true;

        var sel = SelectedRequirement;
        if (ReferenceEquals(sel, requirement))
        {
            SelectedRequirement = null;
            SelectedRequirement = sel;
        }

        RefreshFilters();
        UpdateDashboard();
    }
}

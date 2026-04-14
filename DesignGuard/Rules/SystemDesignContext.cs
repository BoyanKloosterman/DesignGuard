using DesignGuard.Models;

namespace DesignGuard.Rules;

/// <summary>Snapshot van systeemkenmerken voor regels (geen I/O).</summary>
public sealed class SystemDesignContext
{
    public SystemDesignContext(ProjectModel project)
    {
        Project = project;
    }

    public ProjectModel Project { get; }

    public bool HasDatabase =>
        Project.Components.Any(c => TagEquals(c.Tag, "database", "db", "datastore"));

    public bool HasExternalService =>
        Project.ExternalApis ||
        Project.Components.Any(c => TagEquals(c.Tag, "external", "third-party", "saas"));

    public bool HasApiLayer =>
        Project.SystemType is SystemType.Api or SystemType.WebApp or SystemType.MobileBackend ||
        Project.Components.Any(c => TagEquals(c.Tag, "api", "backend", "service"));

    public bool HasFrontend =>
        Project.SystemType == SystemType.WebApp ||
        Project.Components.Any(c => TagEquals(c.Tag, "frontend", "ui", "spa", "web"));

    public bool HasAdminSurface =>
        Project.HasAdmin ||
        Project.Components.Any(c =>
            c.Name.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
            TagEquals(c.Tag, "admin"));

    public bool HasTrustBoundaryCrossing =>
        Project.DataFlows.Any(f =>
        {
            var from = Project.Components.FirstOrDefault(c => c.Id == f.FromComponentId);
            var to = Project.Components.FirstOrDefault(c => c.Id == f.ToComponentId);
            if (from == null || to == null) return false;
            return from.TrustBoundaryId != to.TrustBoundaryId &&
                   from.TrustBoundaryId is not null &&
                   to.TrustBoundaryId is not null;
        });

    public bool InternetFacingHighRisk =>
        Project.InternetExposed && (Project.PersonalDataProcessed || HasAdminSurface || Project.SensitiveDataStored);

    public IReadOnlyList<string> AllTriggerKeys()
    {
        var keys = new List<string>();
        void add(string k) { if (!keys.Contains(k)) keys.Add(k); }

        if (Project.InternetExposed) add(RuleTriggerKeys.InternetExposed);
        if (Project.HasAuthentication) add(RuleTriggerKeys.HasAuthentication);
        if (HasAdminSurface) add(RuleTriggerKeys.AdminSurface);
        if (Project.PersonalDataProcessed) add(RuleTriggerKeys.PersonalData);
        if (Project.SensitiveDataStored) add(RuleTriggerKeys.SensitiveStorage);
        if (HasExternalService) add(RuleTriggerKeys.ExternalIntegration);
        if (Project.FileUpload) add(RuleTriggerKeys.FileUpload);
        if (HasDatabase) add(RuleTriggerKeys.DatabasePresent);
        if (HasApiLayer) add(RuleTriggerKeys.ApiLayer);
        if (HasFrontend) add(RuleTriggerKeys.Frontend);
        if (HasTrustBoundaryCrossing) add(RuleTriggerKeys.TrustBoundaryCrossing);
        if (!Project.LoggingMonitoringPresent) add(RuleTriggerKeys.LoggingMonitoringMissing);
        if (Project.CriticalBusinessFunction) add(RuleTriggerKeys.CriticalBusiness);
        if (Project.InternetExposed && HasAdminSurface) add(RuleTriggerKeys.InternetFacingAdmin);

        return keys;
    }

    private static bool TagEquals(string tag, params string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return allowed.Any(a => tag.Equals(a, StringComparison.OrdinalIgnoreCase));
    }
}

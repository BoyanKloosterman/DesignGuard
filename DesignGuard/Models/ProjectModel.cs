namespace DesignGuard.Models;

/// <summary>
/// Volledige project- en ontwerpstatus voor generatie en export.
/// </summary>
public sealed class ProjectModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string SystemName { get; set; } = "";
    public SystemType SystemType { get; set; } = SystemType.WebApp;
    public DeploymentContext DeploymentContext { get; set; } = DeploymentContext.Cloud;

    /// <summary>Bereikbaar vanaf internet (publiek of remote).</summary>
    public bool InternetExposed { get; set; } = true;

    public bool PersonalDataProcessed { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasAdmin { get; set; }
    public bool ExternalApis { get; set; }
    public bool FileUpload { get; set; }
    public bool SensitiveDataStored { get; set; }
    public bool LoggingMonitoringPresent { get; set; } = true;
    public bool CriticalBusinessFunction { get; set; }

    public List<ComponentModel> Components { get; set; } = new();
    public List<DataFlowModel> DataFlows { get; set; } = new();
    public List<UserRoleModel> UserRoles { get; set; } = new();
    public List<TrustBoundaryModel> TrustBoundaries { get; set; } = new();
    public List<AssetModel> Assets { get; set; } = new();
    public List<DesignNoteModel> DesignNotes { get; set; } = new();
    public List<ControlModel> Controls { get; set; } = new();

    public List<EntryPointModel> EntryPoints { get; set; } = new();
    public List<SensitiveDataModel> SensitiveDataItems { get; set; } = new();
    public List<ReviewItemModel> ReviewItems { get; set; } = new();
    public List<SnapshotModel> Snapshots { get; set; } = new();

    public List<ThreatModel> Threats { get; set; } = new();
    public List<RequirementModel> Requirements { get; set; } = new();

    public string OpenIssuesSummary { get; set; } = "";

    /// <summary>Afgewezen suggesties (rule keys), lokaal opgeslagen.</summary>
    public List<string> DismissedSuggestionKeys { get; set; } = new();
}

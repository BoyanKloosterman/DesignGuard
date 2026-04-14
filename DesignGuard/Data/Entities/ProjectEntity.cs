namespace DesignGuard.Data.Entities;

public sealed class ProjectEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public string SystemName { get; set; } = "";
    public string SystemType { get; set; } = "WebApp";
    public string DeploymentContext { get; set; } = "Cloud";

    public bool InternetExposed { get; set; } = true;

    public bool PersonalDataProcessed { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasAdmin { get; set; }
    public bool ExternalApis { get; set; }
    public bool FileUpload { get; set; }
    public bool SensitiveDataStored { get; set; }
    public bool LoggingMonitoringPresent { get; set; } = true;
    public bool CriticalBusinessFunction { get; set; }

    public string OpenIssuesSummary { get; set; } = "";

    public List<TrustBoundaryEntity> TrustBoundaries { get; set; } = new();
    public List<ComponentEntity> Components { get; set; } = new();
    public List<DataFlowEntity> DataFlows { get; set; } = new();
    public List<UserRoleEntity> UserRoles { get; set; } = new();
    public List<AssetEntity> Assets { get; set; } = new();
    public List<DesignNoteEntity> DesignNotes { get; set; } = new();
    public List<ControlEntity> Controls { get; set; } = new();
    public List<ThreatEntity> Threats { get; set; } = new();
    public List<RequirementEntity> Requirements { get; set; } = new();
}

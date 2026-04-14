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

    public bool PersonalDataProcessed { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasAdmin { get; set; }
    public bool ExternalApis { get; set; }
    public bool FileUpload { get; set; }
    public bool SensitiveDataStored { get; set; }

    public List<ComponentEntity> Components { get; set; } = new();
    public List<DataFlowEntity> DataFlows { get; set; } = new();
    public List<UserRoleEntity> UserRoles { get; set; } = new();
}

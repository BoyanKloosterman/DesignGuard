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

    public bool PersonalDataProcessed { get; set; }
    public bool HasAuthentication { get; set; }
    public bool HasAdmin { get; set; }
    public bool ExternalApis { get; set; }
    public bool FileUpload { get; set; }
    public bool SensitiveDataStored { get; set; }

    public List<ComponentModel> Components { get; set; } = new();
    public List<DataFlowModel> DataFlows { get; set; } = new();
    public List<UserRoleModel> UserRoles { get; set; } = new();
}

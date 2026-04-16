namespace DesignGuard.Data.Entities;

public sealed class SensitiveDataEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public int RelatedComponentId { get; set; }
    public string StorageLocation { get; set; } = "";
    public string Notes { get; set; } = "";
}

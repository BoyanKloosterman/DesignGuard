namespace DesignGuard.Data.Entities;

public sealed class AssetEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Classification { get; set; } = "Unspecified";
    public string Sensitivity { get; set; } = "None";
    public string Notes { get; set; } = "";
    public int RelatedComponentId { get; set; }
    public string RelatedComponentIdsJson { get; set; } = "[]";
}

namespace DesignGuard.Data.Entities;

public sealed class ComponentEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public int? TrustBoundaryId { get; set; }
    public TrustBoundaryEntity? TrustBoundary { get; set; }
    public bool IsEntryPoint { get; set; }
    public string AssetClassification { get; set; } = "Unspecified";
    public string DataSensitivity { get; set; } = "None";
    public string Notes { get; set; } = "";
    public double? VisualX { get; set; }
    public double? VisualY { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tag { get; set; } = "";
}

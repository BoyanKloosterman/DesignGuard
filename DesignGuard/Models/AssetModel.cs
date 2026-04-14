namespace DesignGuard.Models;

public sealed class AssetModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public AssetClassification Classification { get; set; } = AssetClassification.Unspecified;
    public DataSensitivity Sensitivity { get; set; } = DataSensitivity.None;
    public string Notes { get; set; } = "";
    /// <summary>Gekoppeld component-id,0 = geen.</summary>
    public int RelatedComponentId { get; set; }
}

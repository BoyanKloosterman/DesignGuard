namespace DesignGuard.Models;

public sealed class AssetModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Enum-naam of eigen label (DB string).</summary>
    public string Classification { get; set; } = nameof(AssetClassification.Unspecified);

    /// <summary>Enum-naam of eigen label (DB string).</summary>
    public string Sensitivity { get; set; } = nameof(DataSensitivity.None);
    public string Notes { get; set; } = "";
    /// <summary>Gekoppeld component-id,0 = geen.</summary>
    public int RelatedComponentId { get; set; }
}

namespace DesignGuard.Models;

public sealed class ComponentModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Vrije tag, bv. frontend, api, database, external.</summary>
    public string Tag { get; set; } = "";

    public int? TrustBoundaryId { get; set; }
    /// <summary>Fallback koppeling vóór eerste save (wanneer Id nog 0 is).</summary>
    public string? TrustBoundaryName { get; set; }
    public bool IsEntryPoint { get; set; }
    public AssetClassification AssetClassification { get; set; } = AssetClassification.Unspecified;
    public DataSensitivity StoresOrProcesses { get; set; } = DataSensitivity.None;
    public string Notes { get; set; } = "";
    public double? VisualX { get; set; }
    public double? VisualY { get; set; }
}

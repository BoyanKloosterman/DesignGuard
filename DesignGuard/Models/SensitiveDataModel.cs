namespace DesignGuard.Models;

/// <summary>Gecategoriseerde gevoelige data (aanvulling op losse assets).</summary>
public sealed class SensitiveDataModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Bijv. PII, credentials, health, financieel.</summary>
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public int RelatedComponentId { get; set; }
    public string StorageLocation { get; set; } = "";
    public string Notes { get; set; } = "";
}

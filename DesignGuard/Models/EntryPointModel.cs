namespace DesignGuard.Models;

/// <summary>Ingang naar het systeem (los van component-entry vlag; kan ook abstract).</summary>
public sealed class EntryPointModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Gekoppeld component, 0 = niet gekoppeld.</summary>
    public int RelatedComponentId { get; set; }
    public string Notes { get; set; } = "";
    public string ExposureNotes { get; set; } = "";
}

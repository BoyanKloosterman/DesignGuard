namespace DesignGuard.Models;

/// <summary>Ingang afgeleid van componenten met Entry-vlag (opslag/export; niet meer apart in UI).</summary>
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

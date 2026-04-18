namespace DesignGuard.Models;

/// <summary>Aanbevolen maatregel / controle, los van dreigingstekst.</summary>
public sealed class ControlModel
{
    public int Id { get; set; }
    /// <summary>Stabiele id (o.a. koppeling library-template).</summary>
    public string StableId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> SourceTags { get; set; } = new();
    public string Description { get; set; } = "";
    public string ImplementationGuidance { get; set; } = "";
    /// <summary>StableId van gerelateerde dreiging, leeg = algemeen.</summary>
    public string LinkedThreatStableId { get; set; } = "";
    public List<string> LinkedRequirementStableIds { get; set; } = new();
    public ControlLifecycleStatus Status { get; set; } = ControlLifecycleStatus.Draft;
    public string StatusNotes { get; set; } = "";
    /// <summary>Id uit control-library.json (optioneel).</summary>
    public string LibraryDefinitionId { get; set; } = "";

    /// <summary>Gekoppelde componenten (scope van de maatregel).</summary>
    public List<int> LinkedComponentIds { get; set; } = new();
}

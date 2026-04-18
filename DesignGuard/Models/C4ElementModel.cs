namespace DesignGuard.Models;

/// <summary>Één element in het C4-threatmodel-overzicht.</summary>
public sealed class C4ElementModel
{
    public int Id { get; set; }
    public C4Level Level { get; set; } = C4Level.Container;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Technologie of extra scope-notitie (C4-stijl).</summary>
    public string Technology { get; set; } = "";
    public int? ParentId { get; set; }
}

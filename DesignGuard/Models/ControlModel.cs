namespace DesignGuard.Models;

/// <summary>Aanbevolen maatregel / controle, los van dreigingstekst.</summary>
public sealed class ControlModel
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>StableId van gerelateerde dreiging, leeg = algemeen.</summary>
    public string LinkedThreatStableId { get; set; } = "";
    public string StatusNotes { get; set; } = "";
}

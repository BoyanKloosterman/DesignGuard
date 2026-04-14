namespace DesignGuard.Models;

/// <summary>Aannames, beslissingen, constraints en open vragen in één model.</summary>
public sealed class DesignNoteModel
{
    public int Id { get; set; }
    public DesignNoteKind Kind { get; set; } = DesignNoteKind.Assumption;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
}

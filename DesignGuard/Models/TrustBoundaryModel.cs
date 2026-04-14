namespace DesignGuard.Models;

public sealed class TrustBoundaryModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
    /// <summary>Kleur voor diagram (#RRGGBB), optioneel.</summary>
    public string ColorHint { get; set; } = "#4472C4";
}

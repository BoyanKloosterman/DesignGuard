namespace DesignGuard.Models;

public sealed class ThreatModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public StrideCategory StrideCategory { get; set; }
    public string Description { get; set; } = "";
    public List<string> AffectedComponents { get; set; } = new();
    public string GenerationReason { get; set; } = "";
    public List<string> SuggestedMitigations { get; set; } = new();
    public ExplanationModel Explanation { get; set; } = new();
}

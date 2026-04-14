namespace DesignGuard.Models;

public sealed class ThreatModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Stabiele sleutel voor dedup/sync met regels.</summary>
    public string? RuleFingerprint { get; set; }
    public ThreatOrigin Origin { get; set; } = ThreatOrigin.Generated;
    public bool UserModified { get; set; }

    public string Title { get; set; } = "";
    public StrideCategory StrideCategory { get; set; }
    public SeverityEstimate Severity { get; set; } = SeverityEstimate.Medium;
    public ThreatStatus Status { get; set; } = ThreatStatus.Open;
    public string Notes { get; set; } = "";

    public string Description { get; set; } = "";
    public List<string> AffectedComponents { get; set; } = new();
    public List<string> AffectedAssets { get; set; } = new();
    public string GenerationReason { get; set; } = "";
    public List<string> SuggestedMitigations { get; set; } = new();
    public ExplanationModel Explanation { get; set; } = new();

    /// <summary>Kenmerken uit ontwerp dat deze dreiging activeerde (traceability).</summary>
    public List<string> TriggerKeys { get; set; } = new();
    public List<int> RelatedDesignNoteIds { get; set; } = new();
}

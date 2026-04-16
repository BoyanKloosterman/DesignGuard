namespace DesignGuard.Models;

public sealed class RequirementModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? RuleFingerprint { get; set; }
    public RequirementOrigin Origin { get; set; } = RequirementOrigin.Generated;
    public bool UserModified { get; set; }

    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> SourceTags { get; set; } = new();
    public RequirementPriority Priority { get; set; } = RequirementPriority.Medium;
    public RequirementStatus Status { get; set; } = RequirementStatus.Proposed;
    public string Notes { get; set; } = "";

    public string PlainExplanation { get; set; } = "";
    public string WhyApplies { get; set; } = "";
    public string ImplementationDirection { get; set; } = "";
    public ExplanationModel Explanation { get; set; } = new();

    public List<string> TriggerKeys { get; set; } = new();
    public List<string> LinkedThreatIds { get; set; } = new();
    public List<int> RelatedDesignNoteIds { get; set; } = new();

    public SourceAttributionModel SourceAttribution { get; set; } = new();
}

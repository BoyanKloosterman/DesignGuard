namespace DesignGuard.Data.Entities;

public sealed class RequirementEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }

    public string StableId { get; set; } = "";
    public string? RuleFingerprint { get; set; }
    public int Origin { get; set; }
    public bool UserModified { get; set; }

    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceTagsJson { get; set; } = "[]";
    public int Priority { get; set; }
    public int Status { get; set; }

    /// <summary>ISO-8601 UTC of leeg.</summary>
    public string? StatusChangedAtUtc { get; set; }

    public string StatusChangedBy { get; set; } = "";
    public string StatusChangeNote { get; set; } = "";

    public string Notes { get; set; } = "";

    public string PlainExplanation { get; set; } = "";
    public string WhyApplies { get; set; } = "";
    public string ImplementationDirection { get; set; } = "";
    public string TriggerKeysJson { get; set; } = "[]";
    public string LinkedThreatIdsJson { get; set; } = "[]";
    public string ExplanationJson { get; set; } = "{}";
    public string RelatedDesignNoteIdsJson { get; set; } = "[]";

    public string SourceAttributionJson { get; set; } = "{}";
}

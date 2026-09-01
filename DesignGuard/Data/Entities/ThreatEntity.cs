namespace DesignGuard.Data.Entities;

public sealed class ThreatEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }

    public string StableId { get; set; } = "";
    public string? RuleFingerprint { get; set; }
    public int Origin { get; set; }
    public bool UserModified { get; set; }

    public string Title { get; set; } = "";
    public int StrideCategory { get; set; }
    public int Severity { get; set; }
    public int Likelihood { get; set; }
    public int Impact { get; set; }
    public int Status { get; set; }

    /// <summary>ISO-8601 UTC of leeg.</summary>
    public string? StatusChangedAtUtc { get; set; }

    public string StatusChangedBy { get; set; } = "";
    public string StatusChangeNote { get; set; } = "";

    public string Notes { get; set; } = "";

    public string Description { get; set; } = "";
    public string GenerationReason { get; set; } = "";
    public string MitigationsJson { get; set; } = "[]";
    public string AffectedComponentsJson { get; set; } = "[]";
    public string AffectedAssetsJson { get; set; } = "[]";
    public string TriggerKeysJson { get; set; } = "[]";
    public string ExplanationJson { get; set; } = "{}";
    public string RelatedDesignNoteIdsJson { get; set; } = "[]";

    public string SourceAttributionJson { get; set; } = "{}";
}

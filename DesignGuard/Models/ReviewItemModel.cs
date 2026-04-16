namespace DesignGuard.Models;

public sealed class ReviewItemModel
{
    public int Id { get; set; }
    public ReviewSubjectKind SubjectKind { get; set; }
    /// <summary>StableId van dreiging/eis/control of design note id als string.</summary>
    public string SubjectStableId { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public ReviewWorkflowStatus Status { get; set; } = ReviewWorkflowStatus.Draft;
    public string Notes { get; set; } = "";
    public string Rationale { get; set; } = "";
    public string Owner { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

namespace DesignGuard.Data.Entities;

public sealed class ReviewItemEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public int SubjectKind { get; set; }
    public string SubjectStableId { get; set; } = "";
    public string SubjectTitle { get; set; } = "";
    public int Status { get; set; }
    public string Notes { get; set; } = "";
    public string Rationale { get; set; } = "";
    public string Owner { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}

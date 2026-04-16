namespace DesignGuard.Data.Entities;

public sealed class SnapshotEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public string SnapshotJson { get; set; } = "";
}

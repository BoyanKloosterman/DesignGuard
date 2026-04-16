namespace DesignGuard.Models;

public sealed class SnapshotModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Volledige projectserialisatie (JSON) voor diff later.</summary>
    public string SnapshotJson { get; set; } = "";
}

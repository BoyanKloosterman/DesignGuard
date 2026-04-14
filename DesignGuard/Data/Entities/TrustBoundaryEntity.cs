namespace DesignGuard.Data.Entities;

public sealed class TrustBoundaryEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
    public string ColorHint { get; set; } = "#4472C4";
}

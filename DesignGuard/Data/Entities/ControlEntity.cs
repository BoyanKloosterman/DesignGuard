namespace DesignGuard.Data.Entities;

public sealed class ControlEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string LinkedThreatStableId { get; set; } = "";
    public string StatusNotes { get; set; } = "";
}

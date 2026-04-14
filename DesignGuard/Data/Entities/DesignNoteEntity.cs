namespace DesignGuard.Data.Entities;

public sealed class DesignNoteEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public int Kind { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Notes { get; set; } = "";
}

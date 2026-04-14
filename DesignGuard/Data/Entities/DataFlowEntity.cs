namespace DesignGuard.Data.Entities;

public sealed class DataFlowEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public ProjectEntity? Project { get; set; }
    public int FromComponentId { get; set; }
    public ComponentEntity? FromComponent { get; set; }
    public int ToComponentId { get; set; }
    public ComponentEntity? ToComponent { get; set; }
    public string Label { get; set; } = "";
    public string? Notes { get; set; }
}

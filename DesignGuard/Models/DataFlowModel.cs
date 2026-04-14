namespace DesignGuard.Models;

public sealed class DataFlowModel
{
    public int Id { get; set; }
    public int FromComponentId { get; set; }
    public int ToComponentId { get; set; }
    public string Label { get; set; } = "";
    public string? Notes { get; set; }

    /// <summary>Optioneel: voor seed/demo — repository koppelt aan component-Id na insert.</summary>
    public string? SourceComponentName { get; set; }
    public string? TargetComponentName { get; set; }
}

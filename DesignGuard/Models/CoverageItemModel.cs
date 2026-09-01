namespace DesignGuard.Models;

/// <summary>WSTG-thema in testdekking. Geen teststappen of payloads.</summary>
public sealed class CoverageItemModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string WstgRef { get; set; } = "";
    public CoverageStatus Status { get; set; } = CoverageStatus.NotStarted;
    public string Notes { get; set; } = "";

    public bool IsDone => Status is CoverageStatus.Tested or CoverageStatus.Blocked or CoverageStatus.NotApplicable;
    public bool IsBlocked => Status == CoverageStatus.Blocked;
}

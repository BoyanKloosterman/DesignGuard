namespace DesignGuard.Models;

/// <summary>Traceerbaarheid naar knowledge pack en guidance-items (geen compliance-claim).</summary>
public sealed class SourceAttributionModel
{
    public string KnowledgePackId { get; set; } = "";

    public string KnowledgePackVersionLabel { get; set; } = "";

    public string KnowledgePackDisplayLabel { get; set; } = "";

    public List<string> GuidanceItemIds { get; set; } = new();

    public GuidanceNature Nature { get; set; } = GuidanceNature.IndustryGuidanceInspired;

    /// <summary>Leesbare samenvatting voor UI (bijv. officiële titel van bron).</summary>
    public string SourceSummary { get; set; } = "";
}

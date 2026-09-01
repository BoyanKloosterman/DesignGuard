using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Vaste WSTG-thema's voor testdekking. Seed + merge op stabiele id.</summary>
public static class CoverageCatalog
{
    public static IReadOnlyList<CoverageItemModel> Defaults() =>
    [
        Item("cov-auth", "Authenticatie / identity", "WSTG-ATHN"),
        Item("cov-session", "Sessiebeheer", "WSTG-SESS"),
        Item("cov-authz", "Autorisatie", "WSTG-ATHZ"),
        Item("cov-input", "Inputvalidatie", "WSTG-INPV"),
        Item("cov-crypto", "Cryptografie / transport", "WSTG-CRYP"),
        Item("cov-logic", "Business logic", "WSTG-BUSL"),
        Item("cov-api", "API", "WSTG-APIT"),
        Item("cov-errors", "Foutafhandeling / logging", "WSTG-ERRH")
    ];

    public static List<CoverageItemModel> Merge(IEnumerable<CoverageItemModel>? existing)
    {
        var byId = (existing ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.Id))
            .GroupBy(x => x.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var result = new List<CoverageItemModel>();
        foreach (var d in Defaults())
        {
            if (byId.TryGetValue(d.Id, out var old))
            {
                old.Title = d.Title;
                old.WstgRef = d.WstgRef;
                result.Add(old);
            }
            else
            {
                result.Add(d);
            }
        }

        return result;
    }

    public static IReadOnlyList<CoverageItemModel> NotTested(IEnumerable<CoverageItemModel> items) =>
        items.Where(x => x.Status is CoverageStatus.Blocked or CoverageStatus.NotApplicable).ToList();

    public static IReadOnlyList<PentestFindingModel> ResidualFindings(IEnumerable<PentestFindingModel> findings) =>
        findings.Where(f => f.CountsInHeatmap && f.RiskLevel is RiskLevel.High or RiskLevel.Critical).ToList();

    public static string Summary(IEnumerable<CoverageItemModel> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return "Testdekking: nog niet gestart.";
        var tested = list.Count(x => x.Status == CoverageStatus.Tested);
        var blocked = list.Count(x => x.Status == CoverageStatus.Blocked);
        var na = list.Count(x => x.Status == CoverageStatus.NotApplicable);
        return $"Testdekking: {tested}/{list.Count} onderzocht, {blocked} geblokkeerd, {na} n.v.t.";
    }

    private static CoverageItemModel Item(string id, string title, string wstgRef) =>
        new()
        {
            Id = id,
            Title = title,
            WstgRef = wstgRef,
            Status = CoverageStatus.NotStarted
        };
}

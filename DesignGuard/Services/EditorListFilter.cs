using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Filter- en sorteerlogica voor dreigingen- en eisenlijsten (gedeeld door UI en tests).</summary>
public static class EditorListFilter
{
    public const string QuickFilterAlle = "Alle";
    public const string QuickFilterAlleenOpen = "Alleen open";
    public const string QuickFilterAlleenHoog = "Alleen hoog (ernst)";

    public static IReadOnlyList<ThreatModel> FilterAndSortThreats(
        IEnumerable<ThreatModel> threats,
        string? filterText,
        string threatSort,
        string? quickFilter = null)
    {
        IEnumerable<ThreatModel> tq = threats;
        if (!string.IsNullOrWhiteSpace(filterText))
        {
            var f = filterText.Trim();
            tq = tq.Where(t =>
                t.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                t.StrideCategory.ToString().Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(quickFilter, QuickFilterAlleenOpen, StringComparison.OrdinalIgnoreCase))
            tq = tq.Where(t => t.Status == ThreatStatus.Open);
        else if (string.Equals(quickFilter, QuickFilterAlleenHoog, StringComparison.OrdinalIgnoreCase))
            tq = tq.Where(t => t.Severity == SeverityEstimate.High);

        tq = threatSort switch
        {
            "Status" => tq.OrderBy(t => t.Status).ThenBy(t => t.Title),
            "Category" => tq.OrderBy(t => t.StrideCategory).ThenBy(t => t.Title),
            _ => tq.OrderByDescending(t => t.Severity).ThenBy(t => t.Title)
        };

        return tq.ToList();
    }

    public const string ReqQuickFilterAlleenOpen = "Alleen open (niet afgerond)";
    public const string ReqQuickFilterAlleenHoogPrio = "Alleen hoge prioriteit";

    public static IReadOnlyList<RequirementModel> FilterAndSortRequirements(
        IEnumerable<RequirementModel> requirements,
        string? filterText,
        string requirementSort,
        string? quickFilter = null)
    {
        IEnumerable<RequirementModel> rq = requirements;
        if (!string.IsNullOrWhiteSpace(filterText))
        {
            var f = filterText.Trim();
            rq = rq.Where(r =>
                r.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                r.PlainExplanation.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        if (string.Equals(quickFilter, ReqQuickFilterAlleenOpen, StringComparison.OrdinalIgnoreCase))
            rq = rq.Where(r => r.Status is RequirementStatus.Proposed or RequirementStatus.Accepted);
        else if (string.Equals(quickFilter, ReqQuickFilterAlleenHoogPrio, StringComparison.OrdinalIgnoreCase))
            rq = rq.Where(r => r.Priority == RequirementPriority.High);

        rq = requirementSort switch
        {
            "Status" => rq.OrderBy(r => r.Status).ThenBy(r => r.Title),
            "Category" => rq.OrderBy(r => r.Category).ThenBy(r => r.Title),
            _ => rq.OrderByDescending(r => r.Priority).ThenBy(r => r.Title)
        };

        return rq.ToList();
    }
}

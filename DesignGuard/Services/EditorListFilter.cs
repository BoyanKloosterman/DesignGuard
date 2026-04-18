using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Filter- en sorteerlogica voor dreigingen- en eisenlijsten (gedeeld door UI en tests).</summary>
public static class EditorListFilter
{
    public static IReadOnlyList<ThreatModel> FilterAndSortThreats(
        IEnumerable<ThreatModel> threats,
        string? filterText,
        string threatSort)
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

        tq = threatSort switch
        {
            "Status" => tq.OrderBy(t => t.Status).ThenBy(t => t.Title),
            "Category" => tq.OrderBy(t => t.StrideCategory).ThenBy(t => t.Title),
            _ => tq.OrderByDescending(t => t.Severity).ThenBy(t => t.Title)
        };

        return tq.ToList();
    }

    public static IReadOnlyList<RequirementModel> FilterAndSortRequirements(
        IEnumerable<RequirementModel> requirements,
        string? filterText,
        string requirementSort)
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

        rq = requirementSort switch
        {
            "Status" => rq.OrderBy(r => r.Status).ThenBy(r => r.Title),
            "Category" => rq.OrderBy(r => r.Category).ThenBy(r => r.Title),
            _ => rq.OrderByDescending(r => r.Priority).ThenBy(r => r.Title)
        };

        return rq.ToList();
    }
}

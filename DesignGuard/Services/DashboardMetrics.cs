using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Dashboard-tellingen uit dreigingen- en eisencollecties.</summary>
public static class DashboardMetrics
{
    public static (int OpenThreatCount, int MitigatedThreatCount, int OpenRequirementCount, int ImplementedRequirementCount)
        Compute(IEnumerable<ThreatModel> threats, IEnumerable<RequirementModel> requirements)
    {
        var tl = threats as IReadOnlyList<ThreatModel> ?? threats.ToList();
        var rl = requirements as IReadOnlyList<RequirementModel> ?? requirements.ToList();

        var openThreats = 0;
        var mitigated = 0;
        foreach (var t in tl)
        {
            if (t.Status == ThreatStatus.Open) openThreats++;
            else if (t.Status is ThreatStatus.Mitigated or ThreatStatus.Accepted) mitigated++;
        }

        var openReq = 0;
        var implemented = 0;
        foreach (var r in rl)
        {
            if (r.Status is RequirementStatus.Proposed or RequirementStatus.Accepted) openReq++;
            if (r.Status == RequirementStatus.Implemented) implemented++;
        }

        return (openThreats, mitigated, openReq, implemented);
    }
}

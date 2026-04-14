using DesignGuard.Models;
using DesignGuard.Rules;
using DesignGuard.Rules.ThreatRules;

namespace DesignGuard.Services;

public sealed class ThreatGenerationService
{
    private readonly IReadOnlyList<IThreatRule> _rules;

    public ThreatGenerationService(IEnumerable<IThreatRule> rules)
    {
        _rules = rules.ToList();
    }

    public IReadOnlyList<ThreatModel> Generate(ProjectModel project)
    {
        var ctx = new SystemDesignContext(project);
        var list = new List<ThreatModel>();
        var seq = 0;
        foreach (var rule in _rules)
        {
            var ruleName = rule.GetType().Name;
            foreach (var t in rule.Evaluate(ctx))
            {
                if (string.IsNullOrEmpty(t.RuleFingerprint))
                    t.RuleFingerprint = $"{ruleName}:{seq}";
                seq++;
                RuleTriggerBootstrap.Apply(ruleName, t, ctx);
                ApplySeverityHeuristics(ctx, t);
                list.Add(t);
            }
        }

        return DedupeByFingerprint(list);
    }

    private static void ApplySeverityHeuristics(SystemDesignContext ctx, ThreatModel t)
    {
        if (t.Severity == SeverityEstimate.Medium && ctx.InternetFacingHighRisk &&
            t.StrideCategory is StrideCategory.InformationDisclosure or StrideCategory.ElevationOfPrivilege
                or StrideCategory.Spoofing)
            t.Severity = SeverityEstimate.High;

        if (t.Severity == SeverityEstimate.Medium &&
            ctx.Project.InternetExposed && ctx.HasAdminSurface &&
            t.StrideCategory == StrideCategory.ElevationOfPrivilege)
            t.Severity = SeverityEstimate.High;
    }

    private static List<ThreatModel> DedupeByFingerprint(List<ThreatModel> list)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ThreatModel>();
        foreach (var t in list)
        {
            var fp = t.RuleFingerprint ?? t.Title;
            if (!seen.Add(fp))
                continue;
            result.Add(t);
        }

        return result;
    }
}

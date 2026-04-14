using DesignGuard.Models;
using DesignGuard.Rules;
using DesignGuard.Rules.RequirementRules;

namespace DesignGuard.Services;

public sealed class RequirementGenerationService
{
    private readonly IReadOnlyList<IRequirementRule> _rules;

    public RequirementGenerationService(IEnumerable<IRequirementRule> rules)
    {
        _rules = rules.ToList();
    }

    public IReadOnlyList<RequirementModel> Generate(ProjectModel project)
    {
        var ctx = new SystemDesignContext(project);
        var list = new List<RequirementModel>();
        var seq = 0;
        foreach (var rule in _rules)
        {
            var ruleName = rule.GetType().Name;
            foreach (var r in rule.Evaluate(ctx))
            {
                if (string.IsNullOrEmpty(r.RuleFingerprint))
                    r.RuleFingerprint = $"{ruleName}:{seq}";
                seq++;
                RequirementRuleTriggerBootstrap.Apply(ruleName, r, ctx);
                list.Add(r);
            }
        }

        return DedupeByFingerprint(list);
    }

    private static List<RequirementModel> DedupeByFingerprint(List<RequirementModel> list)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<RequirementModel>();
        foreach (var r in list)
        {
            var fp = r.RuleFingerprint ?? r.Title;
            if (!seen.Add(fp))
                continue;
            result.Add(r);
        }

        return result;
    }
}

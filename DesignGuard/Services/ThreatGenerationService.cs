using DesignGuard.Models;
using DesignGuard.Rules;

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
        return _rules.SelectMany(r => r.Evaluate(ctx)).ToList();
    }
}

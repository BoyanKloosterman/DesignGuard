using DesignGuard.Models;
using DesignGuard.Rules;

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
        return _rules.SelectMany(r => r.Evaluate(ctx)).ToList();
    }
}

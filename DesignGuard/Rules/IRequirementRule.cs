using DesignGuard.Models;

namespace DesignGuard.Rules;

public interface IRequirementRule
{
    IEnumerable<RequirementModel> Evaluate(SystemDesignContext ctx);
}

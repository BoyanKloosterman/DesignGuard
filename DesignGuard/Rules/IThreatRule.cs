using DesignGuard.Models;

namespace DesignGuard.Rules;

/// <summary>
/// Uitbreidbaar: nieuwe dreigingsregel = nieuwe implementatie + registratie in DI.
/// </summary>
public interface IThreatRule
{
    IEnumerable<ThreatModel> Evaluate(SystemDesignContext ctx);
}

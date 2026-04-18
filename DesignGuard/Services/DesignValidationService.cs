using DesignGuard.Models;

namespace DesignGuard.Services;

public enum DesignValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record DesignValidationFinding(DesignValidationSeverity Severity, string Code, string Message);

/// <summary>Consistentiechecks op het ontwerp (geen formele normaudit).</summary>
public sealed class DesignValidationService
{
    public IReadOnlyList<DesignValidationFinding> Validate(ProjectModel project)
    {
        var list = new List<DesignValidationFinding>();
        var compIds = new HashSet<int>(project.Components.Where(c => c.Id != 0).Select(c => c.Id));

        if (project.Components.Count > 0 &&
            project.Components.GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
        {
            list.Add(new DesignValidationFinding(DesignValidationSeverity.Warning, "COMP-DUP",
                "Meerdere componenten met dezelfde naam — datastromen en traceability worden onduidelijk."));
        }

        if (string.IsNullOrWhiteSpace(project.Name) && project.Components.Count + project.DataFlows.Count > 0)
        {
            list.Add(new DesignValidationFinding(DesignValidationSeverity.Warning, "PROJ-NAME",
                "Projectnaam is leeg terwijl er ontwerpdata is — vul een naam in voor export en archief."));
        }

        foreach (var f in project.DataFlows)
        {
            if (f.FromComponentId != 0 && !compIds.Contains(f.FromComponentId))
                list.Add(new DesignValidationFinding(DesignValidationSeverity.Error, "FLOW-FROM",
                    $"Datastroom '{f.Label}' verwijst naar onbekend broncomponent (id {f.FromComponentId})."));
            if (f.ToComponentId != 0 && !compIds.Contains(f.ToComponentId))
                list.Add(new DesignValidationFinding(DesignValidationSeverity.Error, "FLOW-TO",
                    $"Datastroom '{f.Label}' verwijst naar onbekend doelcomponent (id {f.ToComponentId})."));
        }

        if (project.InternetExposed && project.Components.Count > 0 &&
            !project.Components.Any(c => c.IsEntryPoint) && project.EntryPoints.Count == 0)
        {
            list.Add(new DesignValidationFinding(DesignValidationSeverity.Warning, "ENTRY-MISS",
                "Internetblootstelling gemarkeerd, maar geen entry points en geen component als entry — overweeg expliciete entry(s)."));
        }

        if (project.TrustBoundaries.Count > 0 && project.Components.Any(c =>
                c.Id != 0 && !c.TrustBoundaryId.HasValue && string.IsNullOrWhiteSpace(c.TrustBoundaryName)))
        {
            list.Add(new DesignValidationFinding(DesignValidationSeverity.Warning, "TB-MAP",
                "Trust boundaries bestaan, maar sommige componenten hebben geen boundary — model is incompleet."));
        }

        if (project.HasAuthentication && project.UserRoles.Count == 0 && project.Components.Count > 0)
        {
            list.Add(new DesignValidationFinding(DesignValidationSeverity.Info, "ROLE-EMPTY",
                "Authenticatie staat aan maar er zijn geen rollen gedefinieerd — vul rollen in voor autorisatie-overzicht."));
        }

        if (list.Count == 0)
            list.Add(new DesignValidationFinding(DesignValidationSeverity.Info, "OK",
                "Geen structurele inconsistenties gevonden (basiscontrole)."));

        return list.OrderByDescending(f => f.Severity).ThenBy(f => f.Code).ToList();
    }
}

using System.Text;
using DesignGuard.Models;
using DesignGuard.Rules;

namespace DesignGuard.Services;

public sealed class TraceabilityExplanation
{
    public string Title { get; init; } = "";
    public IReadOnlyList<string> Lines { get; init; } = Array.Empty<string>();
}

/// <summary>Maakt leesbare traceability-teksten voor UI en export.</summary>
public sealed class TraceabilityService
{
    private static readonly Dictionary<string, string> KeyLabels = new(StringComparer.Ordinal)
    {
        [RuleTriggerKeys.InternetExposed] = "Het systeem is blootgesteld aan internet.",
        [RuleTriggerKeys.HasAuthentication] = "Er is authenticatie in het ontwerp.",
        [RuleTriggerKeys.AdminSurface] = "Er is admin- of beheerfunctionaliteit.",
        [RuleTriggerKeys.PersonalData] = "Er worden persoonsgegevens verwerkt.",
        [RuleTriggerKeys.SensitiveStorage] = "Er wordt gevoelige data opgeslagen.",
        [RuleTriggerKeys.ExternalIntegration] = "Er zijn externe koppelingen of API's.",
        [RuleTriggerKeys.FileUpload] = "Er is bestandsupload.",
        [RuleTriggerKeys.DatabasePresent] = "Er is een database of datastore.",
        [RuleTriggerKeys.ApiLayer] = "Er is een API- of servicelaag.",
        [RuleTriggerKeys.Frontend] = "Er is een frontend of clientlaag.",
        [RuleTriggerKeys.TrustBoundaryCrossing] = "Datastromen kruisen een trust boundary.",
        [RuleTriggerKeys.LoggingMonitoringMissing] = "Logging/monitoring is als afwezig gemarkeerd.",
        [RuleTriggerKeys.CriticalBusiness] = "Bedrijfskritische functionaliteit is gemarkeerd.",
        [RuleTriggerKeys.InternetFacingAdmin] = "Admin en internetblootstelling komen samen voor."
    };

    public TraceabilityExplanation ExplainThreat(ProjectModel project, ThreatModel t)
    {
        var lines = new List<string>();
        foreach (var key in t.TriggerKeys.OrderBy(k => k))
        {
            if (KeyLabels.TryGetValue(key, out var lbl))
                lines.Add(lbl);
            else
                lines.Add($"Kenmerk: {key}");
        }

        if (lines.Count == 0)
            lines.Add("Geen trigger-sleutels vastgelegd; zie generatie-reden.");

        lines.Add($"Regel-reden: {t.GenerationReason}");
        return new TraceabilityExplanation { Title = t.Title, Lines = lines };
    }

    public TraceabilityExplanation ExplainRequirement(ProjectModel project, RequirementModel r)
    {
        var lines = new List<string>();
        foreach (var key in r.TriggerKeys.OrderBy(k => k))
        {
            if (KeyLabels.TryGetValue(key, out var lbl))
                lines.Add(lbl);
            else
                lines.Add($"Kenmerk: {key}");
        }

        lines.Add($"Waarom van toepassing: {r.WhyApplies}");
        if (r.LinkedThreatIds.Count > 0)
        {
            var titles = project.Threats.Where(t => r.LinkedThreatIds.Contains(t.Id)).Select(t => t.Title)
                .ToList();
            if (titles.Count > 0)
                lines.Add("Gerelateerde dreigingen: " + string.Join("; ", titles));
        }

        return new TraceabilityExplanation { Title = r.Title, Lines = lines };
    }

    public string BuildTraceabilitySummary(ProjectModel project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Traceability-overzicht");
        sb.AppendLine();
        sb.AppendLine("### Dreigingen");
        foreach (var t in project.Threats.OrderBy(x => x.StrideCategory).ThenBy(x => x.Title))
        {
            sb.AppendLine($"- **{t.Title}** ({t.StrideCategory}, {t.Severity})");
            foreach (var line in ExplainThreat(project, t).Lines)
                sb.AppendLine($"  - {line}");
            sb.AppendLine();
        }

        sb.AppendLine("### Eisen");
        foreach (var r in project.Requirements.OrderBy(x => x.Category).ThenBy(x => x.Title))
        {
            sb.AppendLine($"- **{r.Title}** [{r.Category}]");
            foreach (var line in ExplainRequirement(project, r).Lines)
                sb.AppendLine($"  - {line}");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}

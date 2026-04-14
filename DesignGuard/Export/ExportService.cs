using System.Text;
using DesignGuard.Models;

namespace DesignGuard.Export;

public sealed class ExportService
{
    public string ToMarkdown(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DesignGuard export");
        sb.AppendLine();
        sb.AppendLine("> Dit document is gegenereerd als **ondersteuning bij Security-by-Design**. ");
        sb.AppendLine("> Het is **geen** juridisch conformiteitsrapport.");
        sb.AppendLine();

        sb.AppendLine("## Project");
        sb.AppendLine($"- **Naam:** {project.Name}");
        sb.AppendLine($"- **Beschrijving:** {project.Description}");
        sb.AppendLine($"- **Laatst bijgewerkt (UTC):** {project.UpdatedAtUtc:O}");
        sb.AppendLine();

        sb.AppendLine("## Systeemoverzicht");
        sb.AppendLine($"- **Systeemnaam:** {project.SystemName}");
        sb.AppendLine($"- **Type:** {project.SystemType}");
        sb.AppendLine($"- **Persoonsgegevens:** {(project.PersonalDataProcessed ? "ja" : "nee")}");
        sb.AppendLine($"- **Authenticatie:** {(project.HasAuthentication ? "ja" : "nee")}");
        sb.AppendLine($"- **Admin:** {(project.HasAdmin ? "ja" : "nee")}");
        sb.AppendLine($"- **Externe API's:** {(project.ExternalApis ? "ja" : "nee")}");
        sb.AppendLine($"- **Uploads:** {(project.FileUpload ? "ja" : "nee")}");
        sb.AppendLine($"- **Gevoelige opslag:** {(project.SensitiveDataStored ? "ja" : "nee")}");
        sb.AppendLine();

        sb.AppendLine("## Componenten");
        foreach (var c in project.Components)
        {
            sb.AppendLine($"- **{c.Name}** (`{c.Tag}`): {c.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("## Datastromen");
        var byId = project.Components.ToDictionary(c => c.Id, c => c.Name);
        foreach (var f in project.DataFlows)
        {
            var from = byId.TryGetValue(f.FromComponentId, out var fn) ? fn : $"#{f.FromComponentId}";
            var to = byId.TryGetValue(f.ToComponentId, out var tn) ? tn : $"#{f.ToComponentId}";
            sb.AppendLine(
                $"- {from} → {to}: **{f.Label}**{(string.IsNullOrWhiteSpace(f.Notes) ? "" : $" — {f.Notes}")}");
        }

        sb.AppendLine();
        sb.AppendLine("## Rollen");
        foreach (var r in project.UserRoles)
        {
            sb.AppendLine($"- **{r.Name}:** {r.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("## STRIDE-dreigingen (regelgebaseerd)");
        foreach (var t in threats)
        {
            sb.AppendLine($"### {t.Title}");
            sb.AppendLine($"- **STRIDE:** {t.StrideCategory}");
            sb.AppendLine($"- **Beschrijving:** {t.Description}");
            sb.AppendLine($"- **Reden:** {t.GenerationReason}");
            sb.AppendLine($"- **Componenten:** {string.Join(", ", t.AffectedComponents)}");
            sb.AppendLine("- **Mitigaties:**");
            foreach (var m in t.SuggestedMitigations)
                sb.AppendLine($"  - {m}");
            sb.AppendLine("- **Uitleg:**");
            sb.AppendLine($"  - *Wat het betekent:* {t.Explanation.WhatItMeans}");
            sb.AppendLine($"  - *Waarom het belangrijk is:* {t.Explanation.WhyItMatters}");
            sb.AppendLine($"  - *Waarom opgenomen:* {t.Explanation.WhyIncluded}");
            sb.AppendLine();
        }

        sb.AppendLine("## Security-eisen (richtinggevend, niet juridisch bindend)");
        foreach (var g in requirements.GroupBy(r => r.Category).OrderBy(g => g.Key))
        {
            sb.AppendLine($"### Thema: {g.Key}");
            foreach (var r in g)
            {
                sb.AppendLine($"#### {r.Title}");
                sb.AppendLine($"- **Bron-tags:** {string.Join(", ", r.SourceTags)}");
                sb.AppendLine($"- **Uitleg:** {r.PlainExplanation}");
                sb.AppendLine($"- **Waarom van toepassing:** {r.WhyApplies}");
                sb.AppendLine($"- **Implementatierichting:** {r.ImplementationDirection}");
                sb.AppendLine("- **Menselijke uitleg:**");
                sb.AppendLine($"  - *Wat het betekent:* {r.Explanation.WhatItMeans}");
                sb.AppendLine($"  - *Waarom het belangrijk is:* {r.Explanation.WhyItMatters}");
                sb.AppendLine($"  - *Waarom opgenomen:* {r.Explanation.WhyIncluded}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Samenvatting");
        sb.AppendLine(
            $"- {project.Components.Count} componenten, {project.DataFlows.Count} datastromen, {project.UserRoles.Count} rollen.");
        sb.AppendLine($"- {threats.Count} gegenereerde dreigingen, {requirements.Count} gegenereerde eisen.");
        sb.AppendLine("- Gebruik dit document als startpunt voor review met je team en stakeholders.");

        return sb.ToString();
    }

    public string ToPlainText(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DESIGNGUARD EXPORT");
        sb.AppendLine("Ondersteunend document — geen juridische conformiteitsclaim.");
        sb.AppendLine();
        sb.AppendLine("PROJECT");
        sb.AppendLine(project.Name);
        sb.AppendLine(project.Description);
        sb.AppendLine();

        sb.AppendLine("SYSTEEM");
        sb.AppendLine($"{project.SystemName} ({project.SystemType})");
        sb.AppendLine();

        sb.AppendLine("COMPONENTEN");
        foreach (var c in project.Components)
            sb.AppendLine($"- {c.Name} [{c.Tag}]: {c.Description}");

        sb.AppendLine();
        sb.AppendLine("DATASTROMEN");
        var byId = project.Components.ToDictionary(c => c.Id, c => c.Name);
        foreach (var f in project.DataFlows)
        {
            var from = byId.TryGetValue(f.FromComponentId, out var fn) ? fn : $"#{f.FromComponentId}";
            var to = byId.TryGetValue(f.ToComponentId, out var tn) ? tn : $"#{f.ToComponentId}";
            sb.AppendLine($"- {from} -> {to}: {f.Label}");
        }

        sb.AppendLine();
        sb.AppendLine("DREIGINGEN");
        foreach (var t in threats)
        {
            sb.AppendLine($"* {t.Title} [{t.StrideCategory}]");
            sb.AppendLine($"  {t.Description}");
            sb.AppendLine($"  Reden: {t.GenerationReason}");
            sb.AppendLine($"  Uitleg: {t.Explanation.WhatItMeans}");
        }

        sb.AppendLine();
        sb.AppendLine("EISEN");
        foreach (var r in requirements)
        {
            sb.AppendLine($"* {r.Title} ({string.Join(", ", r.SourceTags)})");
            sb.AppendLine($"  {r.PlainExplanation}");
        }

        sb.AppendLine();
        sb.AppendLine("SAMENVATTING");
        sb.AppendLine($"{threats.Count} dreigingen, {requirements.Count} eisen.");
        return sb.ToString();
    }
}

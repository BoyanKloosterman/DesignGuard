using System.Text;
using System.Text.Json;
using DesignGuard.Models;

namespace DesignGuard.Export;

public sealed class ExportService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ToMarkdown(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {project.Name} — DesignGuard document");
        sb.AppendLine();
        sb.AppendLine(
            "> **Ondersteuning bij Security-by-Design.** Geen claim op juridische conformiteit (AVG/NIS2/CRA/etc.).");
        sb.AppendLine("> Bron-tags bij eisen zijn **richtinggevend**, geen certificering.");
        sb.AppendLine();

        sb.AppendLine("## Projectoverzicht");
        sb.AppendLine($"- **Naam:** {project.Name}");
        sb.AppendLine($"- **Beschrijving:** {project.Description}");
        sb.AppendLine($"- **Aangemaakt (UTC):** {project.CreatedAtUtc:O}");
        sb.AppendLine($"- **Laatst bijgewerkt (UTC):** {project.UpdatedAtUtc:O}");
        sb.AppendLine();

        sb.AppendLine("## Systeemcontext");
        sb.AppendLine($"- **Systeemnaam:** {project.SystemName}");
        sb.AppendLine($"- **Type:** {project.SystemType}");
        sb.AppendLine($"- **Deployment:** {project.DeploymentContext}");
        sb.AppendLine($"- **Internetblootstelling:** {(project.InternetExposed ? "ja" : "nee")}");
        sb.AppendLine($"- **Persoonsgegevens:** {(project.PersonalDataProcessed ? "ja" : "nee")}");
        sb.AppendLine($"- **Authenticatie:** {(project.HasAuthentication ? "ja" : "nee")}");
        sb.AppendLine($"- **Admin:** {(project.HasAdmin ? "ja" : "nee")}");
        sb.AppendLine($"- **Externe API's / integraties:** {(project.ExternalApis ? "ja" : "nee")}");
        sb.AppendLine($"- **Uploads:** {(project.FileUpload ? "ja" : "nee")}");
        sb.AppendLine($"- **Gevoelige opslag:** {(project.SensitiveDataStored ? "ja" : "nee")}");
        sb.AppendLine($"- **Logging/monitoring (volgens wizard):** {(project.LoggingMonitoringPresent ? "ja" : "nee")}");
        sb.AppendLine($"- **Bedrijfskritisch:** {(project.CriticalBusinessFunction ? "ja" : "nee")}");
        sb.AppendLine();

        if (project.TrustBoundaries.Count > 0)
        {
            sb.AppendLine("## Trust boundaries");
            foreach (var b in project.TrustBoundaries)
                sb.AppendLine($"- **{b.Name}:** {b.Description} {(string.IsNullOrWhiteSpace(b.Notes) ? "" : $"— {b.Notes}")}");
            sb.AppendLine();
        }

        sb.AppendLine("## Componenten");
        foreach (var c in project.Components)
        {
            var tb = c.TrustBoundaryName ?? "";
            if (string.IsNullOrWhiteSpace(tb) && c.TrustBoundaryId is { } tid)
                tb = project.TrustBoundaries.FirstOrDefault(t => t.Id == tid)?.Name ?? "";
            sb.AppendLine(
                $"- **{c.Name}** (`{c.Tag}`){(c.IsEntryPoint ? " *[entry]*" : "")} {(string.IsNullOrWhiteSpace(tb) ? "" : $"— boundary: {tb}")}");
            if (!string.IsNullOrWhiteSpace(c.Description))
                sb.AppendLine($"  - {c.Description}");
            if (c.StoresOrProcesses != DataSensitivity.None)
                sb.AppendLine($"  - Datagevoeligheid (ontwerp): {c.StoresOrProcesses}");
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
            sb.AppendLine($"- **{r.Name}:** {r.Description}");

        if (project.Assets.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Assets");
            foreach (var a in project.Assets)
                sb.AppendLine(
                    $"- **{a.Name}** ({a.Classification}, {a.Sensitivity}): {a.Description} {a.Notes}");
        }

        if (project.DesignNotes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Aannames, beslissingen en open punten");
            foreach (var n in project.DesignNotes.OrderBy(n => n.Kind).ThenBy(n => n.Title))
                sb.AppendLine($"- **[{n.Kind}] {n.Title}:** {n.Description} {n.Notes}");
        }

        sb.AppendLine();
        sb.AppendLine("## Threat model (STRIDE, regelgebaseerd + handmatig)");
        foreach (var t in threats.OrderBy(x => x.StrideCategory).ThenBy(x => x.Title))
        {
            sb.AppendLine($"### {t.Title}");
            sb.AppendLine($"- **STRIDE:** {t.StrideCategory} — **Ernst:** {t.Severity} — **Status:** {t.Status}");
            sb.AppendLine($"- **Herkomst:** {t.Origin}{(t.UserModified ? " (handmatig aangepast)" : "")}");
            sb.AppendLine($"- **Beschrijving:** {t.Description}");
            sb.AppendLine($"- **Waarom gegenereerd / opgenomen:** {t.GenerationReason}");
            if (t.TriggerKeys.Count > 0)
                sb.AppendLine($"- **Triggers (ontwerp):** {string.Join(", ", t.TriggerKeys)}");
            if (t.AffectedComponents.Count > 0)
                sb.AppendLine($"- **Componenten:** {string.Join(", ", t.AffectedComponents)}");
            if (t.AffectedAssets.Count > 0)
                sb.AppendLine($"- **Assets:** {string.Join(", ", t.AffectedAssets)}");
            if (!string.IsNullOrWhiteSpace(t.Notes))
                sb.AppendLine($"- **Notities:** {t.Notes}");
            sb.AppendLine("- **Mitigaties:**");
            foreach (var m in t.SuggestedMitigations)
                sb.AppendLine($"  - {m}");
            sb.AppendLine("- **Uitleg:**");
            sb.AppendLine($"  - *Wat het betekent:* {t.Explanation.WhatItMeans}");
            sb.AppendLine($"  - *Waarom het belangrijk is:* {t.Explanation.WhyItMatters}");
            sb.AppendLine($"  - *Waarom opgenomen:* {t.Explanation.WhyIncluded}");
            sb.AppendLine();
        }

        sb.AppendLine("## Security-eisen (richtinggevend)");
        foreach (var g in requirements.GroupBy(r => r.Category).OrderBy(g => g.Key))
        {
            sb.AppendLine($"### Thema: {g.Key}");
            foreach (var r in g.OrderBy(x => x.Priority).ThenBy(x => x.Title))
            {
                sb.AppendLine($"#### {r.Title}");
                sb.AppendLine(
                    $"- **Prioriteit:** {r.Priority} — **Status:** {r.Status} — **Herkomst:** {r.Origin}");
                sb.AppendLine($"- **Bron-tags (richtinggevend):** {string.Join(", ", r.SourceTags)}");
                sb.AppendLine($"- **Uitleg:** {r.PlainExplanation}");
                sb.AppendLine($"- **Waarom van toepassing:** {r.WhyApplies}");
                sb.AppendLine($"- **Implementatierichting:** {r.ImplementationDirection}");
                if (r.TriggerKeys.Count > 0)
                    sb.AppendLine($"- **Triggers:** {string.Join(", ", r.TriggerKeys)}");
                if (r.LinkedThreatIds.Count > 0)
                {
                    var titles = threats.Where(t => r.LinkedThreatIds.Contains(t.Id)).Select(t => t.Title).ToList();
                    if (titles.Count > 0)
                        sb.AppendLine($"- **Gerelateerde dreigingen:** {string.Join("; ", titles)}");
                }

                if (!string.IsNullOrWhiteSpace(r.Notes))
                    sb.AppendLine($"- **Notities:** {r.Notes}");
                sb.AppendLine("- **Menselijke uitleg:**");
                sb.AppendLine($"  - *Wat het betekent:* {r.Explanation.WhatItMeans}");
                sb.AppendLine($"  - *Waarom het belangrijk is:* {r.Explanation.WhyItMatters}");
                sb.AppendLine($"  - *Waarom opgenomen:* {r.Explanation.WhyIncluded}");
                sb.AppendLine();
            }
        }

        if (project.Controls.Count > 0)
        {
            sb.AppendLine("## Aanbevolen maatregelen (controls)");
            foreach (var c in project.Controls)
                sb.AppendLine(
                    $"- **{c.Title}:** {c.Description} {(string.IsNullOrWhiteSpace(c.LinkedThreatStableId) ? "" : $"(dreiging-id: {c.LinkedThreatStableId})")} {c.StatusNotes}");
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(project.OpenIssuesSummary))
        {
            sb.AppendLine("## Open issues");
            sb.AppendLine(project.OpenIssuesSummary);
            sb.AppendLine();
        }

        sb.AppendLine("## Samenvatting");
        sb.AppendLine(
            $"- {project.Components.Count} componenten, {project.DataFlows.Count} datastromen, {project.UserRoles.Count} rollen, {project.TrustBoundaries.Count} trust boundaries.");
        sb.AppendLine(
            $"- {threats.Count} dreigingen ({threats.Count(t => t.Status == ThreatStatus.Open)} open), {requirements.Count} eisen.");
        sb.AppendLine("- Gebruik dit document als werkdocument voor review — geen compliance-besluit.");

        return sb.ToString();
    }

    public string ToPlainText(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DESIGNGUARD EXPORT");
        sb.AppendLine("Ondersteunend — geen juridische conformiteitsclaim.");
        sb.AppendLine();
        sb.AppendLine("PROJECT");
        sb.AppendLine(project.Name);
        sb.AppendLine(project.Description);
        sb.AppendLine();
        sb.AppendLine("SYSTEEM");
        sb.AppendLine($"{project.SystemName} ({project.SystemType}, {project.DeploymentContext}, internet: {(project.InternetExposed ? "ja" : "nee")})");
        sb.AppendLine();

        sb.AppendLine("TRUST BOUNDARIES");
        foreach (var b in project.TrustBoundaries)
            sb.AppendLine($"- {b.Name}: {b.Description}");

        sb.AppendLine();
        sb.AppendLine("COMPONENTEN");
        foreach (var c in project.Components)
            sb.AppendLine($"- {c.Name} [{c.Tag}] entry={c.IsEntryPoint}");

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
        sb.AppendLine("DESIGNNOTITIES");
        foreach (var n in project.DesignNotes)
            sb.AppendLine($"- [{n.Kind}] {n.Title}: {n.Description}");

        sb.AppendLine();
        sb.AppendLine("DREIGINGEN");
        foreach (var t in threats)
        {
            sb.AppendLine($"* {t.Title} [{t.StrideCategory}] {t.Severity} {t.Status}");
            sb.AppendLine($"  {t.Description}");
            sb.AppendLine($"  Triggers: {string.Join(", ", t.TriggerKeys)}");
        }

        sb.AppendLine();
        sb.AppendLine("EISEN");
        foreach (var r in requirements)
        {
            sb.AppendLine($"* {r.Title} ({r.Category}) prio={r.Priority} status={r.Status}");
            sb.AppendLine($"  Bronnen (richtinggevend): {string.Join(", ", r.SourceTags)}");
            sb.AppendLine($"  {r.PlainExplanation}");
        }

        sb.AppendLine();
        sb.AppendLine("SAMENVATTING");
        sb.AppendLine($"{threats.Count} dreigingen, {requirements.Count} eisen.");
        return sb.ToString();
    }

    public string ToHtml(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>{System.Net.WebUtility.HtmlEncode(project.Name)} — DesignGuard</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;max-width:900px;margin:24px auto;line-height:1.45;}");
        sb.AppendLine("h1,h2,h3{color:#1a365d;} .note{color:#555;font-size:0.95em;} table{border-collapse:collapse;width:100%;}");
        sb.AppendLine("td,th{border:1px solid #ccc;padding:6px;text-align:left;} th{background:#edf2f7;}</style></head><body>");
        sb.AppendLine($"<h1>{Esc(project.Name)}</h1>");
        sb.AppendLine("<p class=\"note\">Ondersteunend document — geen juridische conformiteitsclaim. Bron-tags zijn richtinggevend.</p>");
        sb.AppendLine($"<h2>Systeem</h2><p>{Esc(project.SystemName)} — {project.SystemType} — deployment {project.DeploymentContext}, internet: {(project.InternetExposed ? "ja" : "nee")}</p>");
        sb.AppendLine("<h2>Componenten</h2><table><tr><th>Naam</th><th>Tag</th><th>Entry</th></tr>");
        foreach (var c in project.Components)
            sb.AppendLine($"<tr><td>{Esc(c.Name)}</td><td>{Esc(c.Tag)}</td><td>{(c.IsEntryPoint ? "ja" : "nee")}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("<h2>Dreigingen</h2><table><tr><th>Titel</th><th>STRIDE</th><th>Ernst</th><th>Status</th></tr>");
        foreach (var t in threats)
            sb.AppendLine(
                $"<tr><td>{Esc(t.Title)}</td><td>{t.StrideCategory}</td><td>{t.Severity}</td><td>{t.Status}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("<h2>Eisen</h2><table><tr><th>Titel</th><th>Categorie</th><th>Prioriteit</th><th>Status</th></tr>");
        foreach (var r in requirements)
            sb.AppendLine(
                $"<tr><td>{Esc(r.Title)}</td><td>{Esc(r.Category)}</td><td>{r.Priority}</td><td>{r.Status}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public string ToStructuredJson(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements)
    {
        var doc = new
        {
            schema = "designguard.export.v2",
            project = new
            {
                project.Id,
                project.Name,
                project.Description,
                project.CreatedAtUtc,
                project.UpdatedAtUtc,
                project.SystemName,
                SystemType = project.SystemType.ToString(),
                DeploymentContext = project.DeploymentContext.ToString(),
                project.InternetExposed,
                project.PersonalDataProcessed,
                project.HasAuthentication,
                project.HasAdmin,
                project.ExternalApis,
                project.FileUpload,
                project.SensitiveDataStored,
                project.LoggingMonitoringPresent,
                project.CriticalBusinessFunction,
                project.OpenIssuesSummary,
                TrustBoundaries = project.TrustBoundaries,
                Components = project.Components,
                DataFlows = project.DataFlows,
                UserRoles = project.UserRoles,
                Assets = project.Assets,
                DesignNotes = project.DesignNotes,
                Controls = project.Controls
            },
            threats = threats.Select(t => new
            {
                t.Id,
                t.RuleFingerprint,
                Origin = t.Origin.ToString(),
                t.UserModified,
                t.Title,
                StrideCategory = t.StrideCategory.ToString(),
                Severity = t.Severity.ToString(),
                Status = t.Status.ToString(),
                t.Notes,
                t.Description,
                t.GenerationReason,
                t.SuggestedMitigations,
                t.AffectedComponents,
                t.AffectedAssets,
                t.TriggerKeys,
                t.Explanation,
                t.RelatedDesignNoteIds
            }),
            requirements = requirements.Select(r => new
            {
                r.Id,
                r.RuleFingerprint,
                Origin = r.Origin.ToString(),
                r.UserModified,
                r.Title,
                r.Category,
                r.SourceTags,
                Priority = r.Priority.ToString(),
                Status = r.Status.ToString(),
                r.Notes,
                r.PlainExplanation,
                r.WhyApplies,
                r.ImplementationDirection,
                r.TriggerKeys,
                r.LinkedThreatIds,
                r.Explanation,
                r.RelatedDesignNoteIds
            })
        };
        return JsonSerializer.Serialize(doc, JsonOpts);
    }

    private static string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
}

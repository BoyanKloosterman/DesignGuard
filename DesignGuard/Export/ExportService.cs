using System.Text;
using System.Text.Json;
using DesignGuard.Models;
using DesignGuard.Services;

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

        if (!string.IsNullOrWhiteSpace(project.GovernanceSecurityOwner) ||
            !string.IsNullOrWhiteSpace(project.GovernanceTechnicalOwner) ||
            !string.IsNullOrWhiteSpace(project.GovernanceComplianceStakeholder) ||
            !string.IsNullOrWhiteSpace(project.GovernanceReviewCadence))
        {
            sb.AppendLine("## Governance en organisatie");
            if (!string.IsNullOrWhiteSpace(project.GovernanceSecurityOwner))
                sb.AppendLine($"- **Security-eigenaar:** {project.GovernanceSecurityOwner}");
            if (!string.IsNullOrWhiteSpace(project.GovernanceTechnicalOwner))
                sb.AppendLine($"- **Technische eigenaar:** {project.GovernanceTechnicalOwner}");
            if (!string.IsNullOrWhiteSpace(project.GovernanceComplianceStakeholder))
                sb.AppendLine($"- **Compliance / privacy:** {project.GovernanceComplianceStakeholder}");
            if (!string.IsNullOrWhiteSpace(project.GovernanceReviewCadence))
                sb.AppendLine($"- **Reviewritme:** {project.GovernanceReviewCadence}");
            sb.AppendLine();
        }

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
            if (DesignOntwerpWaarden.ShowsDataSensitivityInExport(c.StoresOrProcesses))
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
            var compById = project.Components.ToDictionary(c => c.Id, c => c.Name);
            foreach (var a in project.Assets)
            {
                a.NormalizeRelatedComponents();
                var compPart = "";
                if (a.RelatedComponentIds.Count > 0)
                {
                    var names = a.RelatedComponentIds.Select(id =>
                        compById.TryGetValue(id, out var n) ? n : $"#{id}");
                    compPart = $" — componenten: {string.Join(", ", names)}";
                }

                sb.AppendLine(
                    $"- **{a.Name}** ({a.Classification}, {a.Sensitivity}){compPart}: {a.Description} {a.Notes}");
            }
        }

        if (project.DesignNotes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Aannames, beslissingen en open punten");
            foreach (var n in project.DesignNotes.OrderBy(n => n.Kind).ThenBy(n => n.Title))
                sb.AppendLine($"- **[{n.Kind}] {n.Title}:** {n.Description} {n.Notes}");
        }

        if (project.C4Elements.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## C4 threatmodel-scope");
            sb.AppendLine(
                "Abstractielagen (C1–C4) voor dit dossier. Koppeling naar dreigingen: dezelfde naam in ‘getroffen componenten’ van een open dreiging als bij het C4-element.");
            foreach (var el in project.C4Elements.OrderBy(x => (int)x.Level).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var openHits = C4ExportPresentation.CountOpenThreatMatchesForComponentName(el.Name, threats);
                var parent = el.ParentId is { } pid ? $" (parent id {pid})" : "";
                sb.AppendLine(
                    $"- **{C4LevelFormatting.ShortLabel(el.Level)}** — **{el.Name}**{parent}: {el.Description} " +
                    $"{(string.IsNullOrWhiteSpace(el.Technology) ? "" : $"— _{el.Technology}_ ")}— open dreigingen met naam-match: **{openHits}**");
            }

            sb.AppendLine();
        }

        sb.AppendLine();
        sb.AppendLine("## Threat model (STRIDE, regelgebaseerd + handmatig)");
        foreach (var t in threats.OrderBy(x => x.StrideCategory).ThenBy(x => x.Title))
        {
            sb.AppendLine($"### {t.Title}");
            sb.AppendLine($"- **STRIDE:** {t.StrideCategory} — **Ernst:** {t.Severity} — **Status:** {t.Status}");
            if (t.StatusChangedAtUtc is { } atUtc)
            {
                var note = string.IsNullOrWhiteSpace(t.StatusChangeNote) ? "" : $" — **Toelichting:** {t.StatusChangeNote}";
                sb.AppendLine(
                    $"- **Status-audit (UTC):** {atUtc:O} — **door:** {t.StatusChangedBy}{note}");
            }

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
                if (r.StatusChangedAtUtc is { } rAtUtc)
                {
                    var rNote = string.IsNullOrWhiteSpace(r.StatusChangeNote) ? "" : $" — **Toelichting:** {r.StatusChangeNote}";
                    sb.AppendLine(
                        $"- **Status-audit (UTC):** {rAtUtc:O} — **door:** {r.StatusChangedBy}{rNote}");
                }

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

        sb.Append(NormativeCoverageService.BuildMarkdownAppendix(project, requirements));

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
            if (r.StatusChangedAtUtc is { } rAud)
                sb.AppendLine(
                    $"  Status-audit: {rAud:O} — {r.StatusChangedBy}{(string.IsNullOrWhiteSpace(r.StatusChangeNote) ? "" : $" — {r.StatusChangeNote}")}");
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

    /// <summary>Print-vriendelijke HTML met secties en disclaimers (geen compliance-claim).</summary>
    public string ToPrintFriendlyHtml(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements,
        DateTime exportUtc)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"nl\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine($"<title>{Esc(project.Name)} — DesignGuard rapport</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("@media print { body { font-size:11pt; } .page-break { page-break-before: always; } }");
        sb.AppendLine("body{font-family:Segoe UI,Arial,sans-serif;max-width:880px;margin:24px auto;line-height:1.5;color:#1e293b;}");
        sb.AppendLine("h1{font-size:22pt;font-weight:600;color:#0f172a;} h2{font-size:14pt;color:#0f172a;margin-top:1.4em;border-bottom:1px solid #cbd5e1;padding-bottom:4px;}");
        sb.AppendLine("h3{font-size:12pt;color:#334155;} .disclaimer{background:#f1f5f9;border-left:4px solid #64748b;padding:12px 16px;margin:16px 0;font-size:10pt;}");
        sb.AppendLine("table{border-collapse:collapse;width:100%;margin:8px 0;} td,th{border:1px solid #e2e8f0;padding:8px;text-align:left;} th{background:#f8fafc;font-weight:600;}");
        sb.AppendLine(".meta{color:#64748b;font-size:10pt;} .tag{font-size:9pt;color:#475569;}</style></head><body>");
        sb.AppendLine($"<h1>{Esc(project.Name)}</h1>");
        sb.AppendLine($"<p class=\"meta\">DesignGuard v5 — export UTC {exportUtc:O} — project: {Esc(project.Name)}</p>");
        sb.AppendLine("<div class=\"disclaimer\"><strong>Belangrijk.</strong> Dit rapport is bedoeld als ondersteuning bij security-by-design. " +
                      "Het is <strong>geen</strong> juridisch advies en maakt <strong>geen</strong> claim op conformiteit met OWASP, GDPR/AVG, NIS2, CRA of andere normen. " +
                      "Knowledge packs en bron-tags zijn richtinggevend; raadpleeg primaire bronnen.</div>");

        sb.AppendLine("<h2>Titelpagina / metadata</h2>");
        sb.AppendLine("<table><tr><th>Veld</th><th>Waarde</th></tr>");
        sb.AppendLine($"<tr><td>Project</td><td>{Esc(project.Name)}</td></tr>");
        sb.AppendLine($"<tr><td>Beschrijving</td><td>{Esc(project.Description)}</td></tr>");
        sb.AppendLine($"<tr><td>Aangemaakt (UTC)</td><td>{project.CreatedAtUtc:O}</td></tr>");
        sb.AppendLine($"<tr><td>Bijgewerkt (UTC)</td><td>{project.UpdatedAtUtc:O}</td></tr></table>");

        if (!string.IsNullOrWhiteSpace(project.GovernanceSecurityOwner) ||
            !string.IsNullOrWhiteSpace(project.GovernanceTechnicalOwner) ||
            !string.IsNullOrWhiteSpace(project.GovernanceComplianceStakeholder) ||
            !string.IsNullOrWhiteSpace(project.GovernanceReviewCadence))
        {
            sb.AppendLine("<h2>Governance</h2><ul>");
            if (!string.IsNullOrWhiteSpace(project.GovernanceSecurityOwner))
                sb.AppendLine($"<li>Security-eigenaar: {Esc(project.GovernanceSecurityOwner)}</li>");
            if (!string.IsNullOrWhiteSpace(project.GovernanceTechnicalOwner))
                sb.AppendLine($"<li>Technisch: {Esc(project.GovernanceTechnicalOwner)}</li>");
            if (!string.IsNullOrWhiteSpace(project.GovernanceComplianceStakeholder))
                sb.AppendLine($"<li>Compliance/privacy: {Esc(project.GovernanceComplianceStakeholder)}</li>");
            if (!string.IsNullOrWhiteSpace(project.GovernanceReviewCadence))
                sb.AppendLine($"<li>Review: {Esc(project.GovernanceReviewCadence)}</li>");
            sb.AppendLine("</ul>");
        }

        sb.AppendLine("<h2>Executive summary</h2>");
        sb.AppendLine("<p>");
        sb.AppendLine(
            $"{project.Components.Count} componenten, {project.DataFlows.Count} datastromen, {threats.Count} dreigingen, " +
            $"{requirements.Count} eisen, {project.Controls.Count} controls, {project.TrustBoundaries.Count} trust boundaries.");
        sb.AppendLine("</p>");

        sb.AppendLine("<h2>Projectoverzicht en systeemcontext</h2>");
        sb.AppendLine("<table><tr><th>Kenmerk</th><th>Waarde</th></tr>");
        sb.AppendLine($"<tr><td>Systeemnaam</td><td>{Esc(project.SystemName)}</td></tr>");
        sb.AppendLine($"<tr><td>Type / deployment</td><td>{project.SystemType} / {project.DeploymentContext}</td></tr>");
        sb.AppendLine(
            $"<tr><td>Flags</td><td>Internet {(project.InternetExposed ? "ja" : "nee")}, persoonsgegevens {(project.PersonalDataProcessed ? "ja" : "nee")}, " +
            $"auth {(project.HasAuthentication ? "ja" : "nee")}, admin {(project.HasAdmin ? "ja" : "nee")}</td></tr></table>");

        sb.AppendLine("<h2>Trust boundaries</h2><ul>");
        foreach (var b in project.TrustBoundaries)
            sb.AppendLine($"<li><strong>{Esc(b.Name)}</strong> — {Esc(b.Description)}</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Componenten en datastromen</h2>");
        sb.AppendLine("<h3>Componenten</h3><table><tr><th>Naam</th><th>Tag</th><th>Entry</th><th>Data</th></tr>");
        foreach (var c in project.Components)
            sb.AppendLine(
                $"<tr><td>{Esc(c.Name)}</td><td>{Esc(c.Tag)}</td><td>{(c.IsEntryPoint ? "ja" : "nee")}</td><td>{c.StoresOrProcesses}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("<h3>Datastromen</h3><ul>");
        var byId = project.Components.ToDictionary(c => c.Id, c => c.Name);
        foreach (var f in project.DataFlows)
        {
            var from = byId.TryGetValue(f.FromComponentId, out var fn) ? fn : $"#{f.FromComponentId}";
            var to = byId.TryGetValue(f.ToComponentId, out var tn) ? tn : $"#{f.ToComponentId}";
            sb.AppendLine($"<li>{Esc(from)} → {Esc(to)}: <strong>{Esc(f.Label)}</strong></li>");
        }

        sb.AppendLine("</ul><div class=\"page-break\"></div>");

        sb.AppendLine("<h2>Assets en gevoelige data</h2><h3>Assets</h3><ul>");
        var compByIdHtml = project.Components.ToDictionary(c => c.Id, c => c.Name);
        foreach (var a in project.Assets)
        {
            a.NormalizeRelatedComponents();
            var compPart = "";
            if (a.RelatedComponentIds.Count > 0)
            {
                var names = a.RelatedComponentIds.Select(id =>
                    compByIdHtml.TryGetValue(id, out var n) ? Esc(n) : Esc($"#{id}"));
                compPart = $" — {string.Join(", ", names)}";
            }

            sb.AppendLine($"<li>{Esc(a.Name)} ({a.Classification}, {a.Sensitivity}){compPart}</li>");
        }

        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Threat model</h2>");
        foreach (var t in threats.OrderBy(x => x.StrideCategory).ThenBy(x => x.Title))
        {
            sb.AppendLine($"<h3>{Esc(t.Title)}</h3>");
            sb.AppendLine($"<p class=\"tag\">{t.StrideCategory} — {t.Severity} — {t.Status} — herkomst {t.Origin}</p>");
            sb.AppendLine($"<p>{Esc(t.Description)}</p>");
            if (t.StatusChangedAtUtc is { } audHtml)
            {
                var note = string.IsNullOrWhiteSpace(t.StatusChangeNote) ? "" : $" — {Esc(t.StatusChangeNote)}";
                sb.AppendLine(
                    $"<p class=\"tag\">Status-audit: {audHtml:O} — {Esc(t.StatusChangedBy)}{note}</p>");
            }

            if (!string.IsNullOrWhiteSpace(t.SourceAttribution.KnowledgePackId))
                sb.AppendLine(
                    $"<p class=\"tag\">Bronspoor: {Esc(t.SourceAttribution.KnowledgePackDisplayLabel)} ({Esc(t.SourceAttribution.KnowledgePackVersionLabel)}) — " +
                    $"{string.Join(", ", t.SourceAttribution.GuidanceItemIds)} — {t.SourceAttribution.Nature}</p>");
        }

        sb.AppendLine("<h2>Security-eisen</h2>");
        foreach (var g in requirements.GroupBy(r => r.Category).OrderBy(g => g.Key))
        {
            sb.AppendLine($"<h3>{Esc(g.Key)}</h3>");
            foreach (var r in g.OrderBy(x => x.Priority).ThenBy(x => x.Title))
            {
                sb.AppendLine($"<h4>{Esc(r.Title)}</h4>");
                sb.AppendLine($"<p>{Esc(r.PlainExplanation)}</p>");
                sb.AppendLine($"<p class=\"tag\">Prioriteit {r.Priority}, status {r.Status}, tags: {string.Join(", ", r.SourceTags)}</p>");
                if (r.StatusChangedAtUtc is { } rAudHtml)
                {
                    var rNote = string.IsNullOrWhiteSpace(r.StatusChangeNote) ? "" : $" — {Esc(r.StatusChangeNote)}";
                    sb.AppendLine(
                        $"<p class=\"tag\">Status-audit: {rAudHtml:O} — {Esc(r.StatusChangedBy)}{rNote}</p>");
                }

                if (!string.IsNullOrWhiteSpace(r.SourceAttribution.KnowledgePackId))
                    sb.AppendLine(
                        $"<p class=\"tag\">Bronspoor: {Esc(r.SourceAttribution.KnowledgePackDisplayLabel)} — items {string.Join(", ", r.SourceAttribution.GuidanceItemIds)} — {r.SourceAttribution.Nature}</p>");
            }
        }

        sb.AppendLine("<h2>Controls</h2><ul>");
        foreach (var c in project.Controls)
            sb.AppendLine($"<li><strong>{Esc(c.Title)}</strong>: {Esc(c.Description)}</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Beslissingen, aannames, open punten</h2><ul>");
        foreach (var n in project.DesignNotes.OrderBy(n => n.Kind).ThenBy(n => n.Title))
            sb.AppendLine($"<li>[{n.Kind}] <strong>{Esc(n.Title)}</strong>: {Esc(n.Description)}</li>");
        if (!string.IsNullOrWhiteSpace(project.OpenIssuesSummary))
            sb.AppendLine($"<li><strong>Open issues</strong>: {Esc(project.OpenIssuesSummary)}</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h2>Normatieve dekking (indicatief)</h2>");
        sb.AppendLine("<div class=\"disclaimer\">" + Esc(
                          "Samenvatting van bron-tags op eisen — geen volledige ASVS/NIST/AVG/CRA-dekking. Zie ook markdown-export voor tabel.") +
                      "</div>");

        sb.AppendLine("<h2>Disclaimer (herhaling)</h2>");
        sb.AppendLine("<div class=\"disclaimer\">Geen juridische conformiteit of certificering. Gebruik primaire bronnen voor audits.</div>");
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
            schema = "designguard.export.v3",
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
                project.GovernanceSecurityOwner,
                project.GovernanceTechnicalOwner,
                project.GovernanceComplianceStakeholder,
                project.GovernanceReviewCadence,
                c4Elements = project.C4Elements,
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
                statusChangedAtUtc = t.StatusChangedAtUtc,
                t.StatusChangedBy,
                t.StatusChangeNote,
                t.Notes,
                t.Description,
                t.GenerationReason,
                t.SuggestedMitigations,
                t.AffectedComponents,
                t.AffectedAssets,
                t.TriggerKeys,
                t.Explanation,
                t.RelatedDesignNoteIds,
                sourceAttribution = t.SourceAttribution
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
                statusChangedAtUtc = r.StatusChangedAtUtc,
                r.StatusChangedBy,
                r.StatusChangeNote,
                r.Notes,
                r.PlainExplanation,
                r.WhyApplies,
                r.ImplementationDirection,
                r.TriggerKeys,
                r.LinkedThreatIds,
                r.Explanation,
                r.RelatedDesignNoteIds,
                sourceAttribution = r.SourceAttribution
            })
        };
        return JsonSerializer.Serialize(doc, JsonOpts);
    }

    private static string Esc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");
}

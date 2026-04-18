using DesignGuard.Knowledge;
using DesignGuard.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DesignGuard.Export;

public sealed class PdfReportService
{
    private const int PdfThreatListMax = 120;
    private const int PdfRequirementListMax = 150;
    private const int PdfTraceabilityTriggerMax = 60;

    private readonly KnowledgePackService _packs;

    public PdfReportService(KnowledgePackService packs)
    {
        _packs = packs;
    }

    public byte[] BuildSecurityDesignReport(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements,
        byte[]? diagramPng,
        byte[]? c4OverviewPng)
    {
        _packs.Reload();
        var exportUtc = DateTime.UtcNow;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10.5f));
                page.Header().Text("DesignGuard — Security-by-design rapport").SemiBold().FontSize(16);
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Pagina ");
                    t.CurrentPageNumber();
                    t.Span(" / ");
                    t.TotalPages();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text($"Project: {project.Name}").SemiBold().FontSize(13);
                    col.Item().Text($"Export (UTC): {exportUtc:O}");
                    col.Item().Text(
                        "Disclaimer: dit document is ondersteunend en maakt geen juridische conformiteit of certificering waar. " +
                        "Bron-tags en knowledge packs zijn richtinggevend; controleer altijd primaire bronnen.");

                    col.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    col.Item().Text("Executive summary").SemiBold().FontSize(12);
                    col.Item().Text(
                        $"Componenten: {project.Components.Count}, datastromen: {project.DataFlows.Count}, " +
                        $"C4-elementen: {project.C4Elements.Count}, dreigingen: {threats.Count}, eisen: {requirements.Count}, " +
                        $"controls: {project.Controls.Count}.");
                    col.Item().Text(
                            "PDF toont beperkte lengtes voor dreigingen/eisen/traceability. Volledige inhoud: export Markdown of JSON.")
                        .FontSize(9.5f).FontColor(Colors.Grey.Darken2);

                    col.Item().Text("Systeemcontext").SemiBold().FontSize(12);
                    col.Item().Text(
                        $"{project.SystemName} — type {project.SystemType}, deployment {project.DeploymentContext}. " +
                        $"Internet: {(project.InternetExposed ? "ja" : "nee")}, persoonsgegevens: {(project.PersonalDataProcessed ? "ja" : "nee")}.");

                    if (diagramPng is { Length: > 0 })
                    {
                        col.Item().Text("Architectuurdiagram (export)").SemiBold().FontSize(12);
                        col.Item().Image(diagramPng).FitArea();
                    }

                    col.Item().Text("Trust boundaries").SemiBold().FontSize(12);
                    foreach (var b in project.TrustBoundaries)
                        col.Item().Text($"• {b.Name}: {b.Description}");

                    if (c4OverviewPng is { Length: > 0 })
                    {
                        col.Item().Text("C4-overzicht (visualisatie)").SemiBold().FontSize(12);
                        col.Item().Image(c4OverviewPng).FitArea();
                    }

                    col.Item().Text("C4 threatmodel-scope").SemiBold().FontSize(12);
                    col.Item().Text(
                        "Het C4-model (Simon Brown) heeft vier zoomniveaus: van context tot code. In DesignGuard vullen " +
                        "we dit los van het architectuurcanvas; het helpt om dreigingen te koppelen aan benoemde onderdelen.");
                    col.Item().Text(
                        "Koppeling naar dreigingen: gebruik exact dezelfde naam in ‘getroffen componenten’ van een open dreiging " +
                        "als bij het C4-element hieronder. De kolom ‘open dreig.’ telt die matches.");

                    foreach (var lvl in new[]
                             {
                                 C4Level.Context, C4Level.Container, C4Level.Component, C4Level.Code
                             })
                    {
                        col.Item().PaddingLeft(6).Text($"{C4LevelFormatting.ShortLabel(lvl)} — {C4LevelFormatting.LevelScopeExplanation(lvl)}")
                            .FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                    }

                    if (project.C4Elements.Count == 0)
                    {
                        col.Item().Text("— Geen C4-elementen vastgelegd in dit dossier.").FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        var idToName = C4ExportPresentation.BuildIdToNameMap(project.C4Elements);

                        foreach (var lvl in new[]
                                 {
                                     C4Level.Context, C4Level.Container, C4Level.Component, C4Level.Code
                                 })
                        {
                            var els = project.C4Elements
                                .Where(e => e.Level == lvl)
                                .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                            if (els.Count == 0)
                                continue;

                            col.Item().Text(C4LevelFormatting.ShortLabel(lvl)).SemiBold().FontSize(11);
                            foreach (var el in els)
                            {
                                var openHits = C4ExportPresentation.CountOpenThreatNameMatches(el, threats);
                                var parentHint = C4ExportPresentation.FormatC4ParentHintPdf(el, idToName);
                                var tech = string.IsNullOrWhiteSpace(el.Technology) ? "" : $" — Tech/notitie: {el.Technology}";
                                col.Item().PaddingLeft(10).Text($"{el.Name} (id {el.Id}){parentHint}{tech} — open dreig. met naam-match: {openHits}")
                                    .SemiBold();
                                if (!string.IsNullOrWhiteSpace(el.Description))
                                    col.Item().PaddingLeft(18).Text(el.Description);
                            }
                        }
                    }

                    col.Item().Text("Dreigingen (selectie)").SemiBold().FontSize(12);
                    foreach (var t in threats.OrderBy(x => x.Title).Take(PdfThreatListMax))
                    {
                        col.Item().Text($"{t.Title} — {t.StrideCategory}, {t.Severity}, {t.Status}").SemiBold();
                        col.Item().PaddingLeft(12).Text(t.Description);
                    }

                    if (threats.Count > PdfThreatListMax)
                        col.Item().Text($"... en {threats.Count - PdfThreatListMax} extra (Markdown/JSON-export).");

                    col.Item().Text("Security-eisen (selectie)").SemiBold().FontSize(12);
                    foreach (var r in requirements.OrderBy(x => x.Category).ThenBy(x => x.Title).Take(PdfRequirementListMax))
                    {
                        col.Item().Text($"{r.Title} [{r.Category}] — {r.Priority}, {r.Status}").SemiBold();
                        col.Item().PaddingLeft(12).Text(r.PlainExplanation);
                        if (!string.IsNullOrWhiteSpace(r.SourceAttribution.KnowledgePackId))
                            col.Item().PaddingLeft(12).Text(
                                $"Bronspoor: pack {r.SourceAttribution.KnowledgePackDisplayLabel} ({r.SourceAttribution.KnowledgePackVersionLabel}), " +
                                $"items: {string.Join(", ", r.SourceAttribution.GuidanceItemIds)} — {r.SourceAttribution.Nature}")
                                .FontColor(Colors.Grey.Darken2);
                    }

                    if (requirements.Count > PdfRequirementListMax)
                        col.Item().Text($"... en {requirements.Count - PdfRequirementListMax} extra (Markdown/JSON-export).");

                    col.Item().Text("Traceability (trigger-sleutels)").SemiBold().FontSize(12);
                    col.Item().Text(
                            "Korte koppeling ontwerp → dreiging/eis. Uitgebreide toelichting: tab Traceability en Markdown-export.")
                        .FontSize(9.5f).FontColor(Colors.Grey.Darken2);
                    foreach (var t in threats.OrderBy(x => x.StrideCategory).ThenBy(x => x.Title).Take(PdfTraceabilityTriggerMax))
                    {
                        var keys = t.TriggerKeys.Count > 0 ? string.Join(", ", t.TriggerKeys) : "—";
                        col.Item().PaddingLeft(8).Text($"Dreiging — {t.Title}: {keys}").FontSize(9.5f);
                    }

                    if (threats.Count > PdfTraceabilityTriggerMax)
                        col.Item().Text($"... {threats.Count - PdfTraceabilityTriggerMax} extra dreigingen.")
                            .FontColor(Colors.Grey.Medium);

                    foreach (var r in requirements.OrderBy(x => x.Category).ThenBy(x => x.Title).Take(PdfTraceabilityTriggerMax))
                    {
                        var keys = r.TriggerKeys.Count > 0 ? string.Join(", ", r.TriggerKeys) : "—";
                        col.Item().PaddingLeft(8).Text($"Eis — {r.Title}: {keys}").FontSize(9.5f);
                    }

                    if (requirements.Count > PdfTraceabilityTriggerMax)
                        col.Item().Text($"... {requirements.Count - PdfTraceabilityTriggerMax} extra eisen.")
                            .FontColor(Colors.Grey.Medium);

                    col.Item().Text("Controls").SemiBold().FontSize(12);
                    foreach (var c in project.Controls)
                        col.Item().Text($"• {c.Title}: {c.Description}");

                    col.Item().Text("Beslissingen en aannames").SemiBold().FontSize(12);
                    foreach (var n in project.DesignNotes.OrderBy(x => x.Kind).ThenBy(x => x.Title))
                        col.Item().Text($"[{n.Kind}] {n.Title}: {n.Description}");

                    col.Item().Text("Knowledge packs (actief geladen)").SemiBold().FontSize(12);
                    foreach (var p in _packs.LoadedPacks)
                        col.Item().Text($"• {p.Dto.DisplayLabel} — v{p.Dto.VersionLabel} — {p.Dto.SourceName}");

                    col.Item().Text("Open issues").SemiBold().FontSize(12);
                    col.Item().Text(string.IsNullOrWhiteSpace(project.OpenIssuesSummary)
                        ? "—"
                        : project.OpenIssuesSummary);
                });
            });
        }).GeneratePdf();
    }
}

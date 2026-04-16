using DesignGuard.Knowledge;
using DesignGuard.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DesignGuard.Export;

public sealed class PdfReportService
{
    private readonly KnowledgePackService _packs;

    public PdfReportService(KnowledgePackService packs)
    {
        _packs = packs;
    }

    public byte[] BuildSecurityDesignReport(
        ProjectModel project,
        IReadOnlyList<ThreatModel> threats,
        IReadOnlyList<RequirementModel> requirements,
        byte[]? diagramPng)
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
                        $"dreigingen: {threats.Count}, eisen: {requirements.Count}, controls: {project.Controls.Count}.");

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

                    col.Item().Text("Dreigingen (selectie)").SemiBold().FontSize(12);
                    foreach (var t in threats.OrderBy(x => x.Title).Take(40))
                    {
                        col.Item().Text($"{t.Title} — {t.StrideCategory}, {t.Severity}, {t.Status}").SemiBold();
                        col.Item().PaddingLeft(12).Text(t.Description);
                    }

                    if (threats.Count > 40)
                        col.Item().Text($"... en {threats.Count - 40} extra (zie export JSON/Markdown).");

                    col.Item().Text("Security-eisen (selectie)").SemiBold().FontSize(12);
                    foreach (var r in requirements.OrderBy(x => x.Category).ThenBy(x => x.Title).Take(50))
                    {
                        col.Item().Text($"{r.Title} [{r.Category}] — {r.Priority}, {r.Status}").SemiBold();
                        col.Item().PaddingLeft(12).Text(r.PlainExplanation);
                        if (!string.IsNullOrWhiteSpace(r.SourceAttribution.KnowledgePackId))
                            col.Item().PaddingLeft(12).Text(
                                $"Bronspoor: pack {r.SourceAttribution.KnowledgePackDisplayLabel} ({r.SourceAttribution.KnowledgePackVersionLabel}), " +
                                $"items: {string.Join(", ", r.SourceAttribution.GuidanceItemIds)} — {r.SourceAttribution.Nature}")
                                .FontColor(Colors.Grey.Darken2);
                    }

                    if (requirements.Count > 50)
                        col.Item().Text($"... en {requirements.Count - 50} extra.");

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

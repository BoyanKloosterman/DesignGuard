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

    // Zakelijke kleuren (donkerblauw / neutraal)
    private static class PdfPalette
    {
        public static readonly Color Primary = Color.FromHex("#1a365d");
        public static readonly Color Accent = Color.FromHex("#2c5282");
        public static readonly Color Muted = Color.FromHex("#4a5568");
        public static readonly Color LightBg = Color.FromHex("#f7fafc");
        public static readonly Color Border = Color.FromHex("#e2e8f0");
        public static readonly Color Link = Color.FromHex("#2b6cb0");
    }

    private static class Sec
    {
        public const string Summary = "dg-sec-summary";
        public const string SystemContext = "dg-sec-system";
        public const string ArchDiagram = "dg-sec-arch";
        public const string TrustBoundaries = "dg-sec-trust";
        public const string C4Visual = "dg-sec-c4vis";
        public const string C4Scope = "dg-sec-c4scope";
        public const string Threats = "dg-sec-threats";
        public const string Requirements = "dg-sec-reqs";
        public const string Traceability = "dg-sec-trace";
        public const string Controls = "dg-sec-controls";
        public const string Decisions = "dg-sec-decisions";
        public const string Packs = "dg-sec-packs";
        public const string OpenIssues = "dg-sec-issues";
    }

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
                page.MarginHorizontal(44);
                page.MarginVertical(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(10.5f).FontColor(PdfPalette.Muted));
                page.PageColor(PdfPalette.LightBg);

                page.Header().SkipOnce().BorderBottom(1).BorderColor(PdfPalette.Border).PaddingBottom(8).Row(row =>
                {
                    row.RelativeItem().Text("DesignGuard").FontSize(9).SemiBold().FontColor(PdfPalette.Primary);
                    row.RelativeItem().AlignRight().Text("Security-by-design rapport").FontSize(9).FontColor(PdfPalette.Muted);
                });

                page.Footer().BorderTop(1).BorderColor(PdfPalette.Border).PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Text(text =>
                    {
                        text.Span(project.Name).SemiBold().FontSize(8.5f).FontColor(PdfPalette.Primary);
                        text.Span(" · ").FontSize(8.5f).FontColor(PdfPalette.Muted);
                        text.Span($"{exportUtc:yyyy-MM-dd HH:mm} UTC").FontSize(8.5f).FontColor(PdfPalette.Muted);
                    });
                    row.ConstantItem(72).AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8.5f).FontColor(PdfPalette.Muted));
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });

                page.Content().Column(col =>
                {
                    col.Spacing(0);

                    // Voorpagina
                    col.Item().PaddingTop(32).PaddingBottom(8).Column(cover =>
                    {
                        cover.Item().Text("DESIGNGUARD").FontSize(10).SemiBold().LetterSpacing(1.2f).FontColor(PdfPalette.Accent);
                        cover.Item().PaddingTop(20).Text("Security-by-design rapport")
                            .FontSize(26).Bold().FontColor(PdfPalette.Primary);
                        cover.Item().PaddingTop(10).Text(project.Name).FontSize(17).SemiBold().FontColor(PdfPalette.Muted);
                        cover.Item().PaddingTop(6).Text($"Export (UTC): {exportUtc:yyyy-MM-dd HH:mm}")
                            .FontSize(10).FontColor(PdfPalette.Muted);
                        cover.Item().PaddingTop(36).Background(Colors.White).Border(1).BorderColor(PdfPalette.Border)
                            .Padding(16).Text(
                                "Disclaimer: dit document is ondersteunend en maakt geen juridische conformiteit of certificering waar. " +
                                "Bron-tags en knowledge packs zijn richtinggevend; controleer altijd primaire bronnen.")
                            .FontSize(9.5f).LineHeight(1.35f).FontColor(PdfPalette.Muted);
                    });

                    col.Item().PageBreak();

                    // Inhoudsopgave
                    col.Item().Text("Inhoudsopgave").FontSize(18).Bold().FontColor(PdfPalette.Primary);
                    col.Item().PaddingTop(4).Text("Klik op een regel om naar het hoofdstuk te gaan.")
                        .FontSize(9.5f).FontColor(PdfPalette.Muted);
                    col.Item().PaddingTop(14);

                    AddTocLine(col, Sec.Summary, "Executive summary");
                    AddTocLine(col, Sec.SystemContext, "Systeemcontext");
                    if (diagramPng is { Length: > 0 })
                        AddTocLine(col, Sec.ArchDiagram, "Architectuurdiagram (Mermaid)");
                    AddTocLine(col, Sec.TrustBoundaries, "Trust boundaries");
                    if (c4OverviewPng is { Length: > 0 })
                        AddTocLine(col, Sec.C4Visual, "C4-overzicht (visualisatie)");
                    AddTocLine(col, Sec.C4Scope, "C4 threatmodel-scope");
                    AddTocLine(col, Sec.Threats, "Dreigingen (selectie)");
                    AddTocLine(col, Sec.Requirements, "Security-eisen (selectie)");
                    AddTocLine(col, Sec.Traceability, "Traceability (trigger-sleutels)");
                    AddTocLine(col, Sec.Controls, "Controls");
                    AddTocLine(col, Sec.Decisions, "Beslissingen en aannames");
                    AddTocLine(col, Sec.Packs, "Knowledge packs (actief geladen)");
                    AddTocLine(col, Sec.OpenIssues, "Open issues");

                    col.Item().PageBreak();

                    // Hoofdstukken
                    col.Item().Section(Sec.Summary).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Executive summary"));
                        s.Item().Text(
                            $"Componenten: {project.Components.Count}, datastromen: {project.DataFlows.Count}, " +
                            $"C4-elementen: {project.C4Elements.Count}, dreigingen: {threats.Count}, eisen: {requirements.Count}, " +
                            $"controls: {project.Controls.Count}.");
                        s.Item().Text(
                                "PDF toont beperkte lengtes voor dreigingen/eisen/traceability. Volledige inhoud: export Markdown of JSON.")
                            .FontSize(9.5f).FontColor(PdfPalette.Muted);
                    });

                    col.Item().Section(Sec.SystemContext).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Systeemcontext"));
                        s.Item().Text(
                            $"{project.SystemName} — type {project.SystemType}, deployment {project.DeploymentContext}. " +
                            $"Internet: {(project.InternetExposed ? "ja" : "nee")}, persoonsgegevens: {(project.PersonalDataProcessed ? "ja" : "nee")}.");
                    });

                    if (diagramPng is { Length: > 0 })
                    {
                        col.Item().Section(Sec.ArchDiagram).Column(s =>
                        {
                            s.Spacing(10);
                            s.Item().Element(SectionTitle("Architectuurdiagram"));
                            s.Item().Text(
                                    "Zelfde Mermaid-flowchart als onder Ontwerp (trust boundaries, componenten, datastromen; " +
                                    "stijlen voor entry point en gevoelige data). Gerasterd uit de WebView2-preview bij PDF-export.")
                                .FontSize(9.5f).LineHeight(1.3f).FontColor(PdfPalette.Muted);
                            s.Item().PaddingTop(4).MinHeight(280).Image(diagramPng).FitArea();
                        });
                    }

                    col.Item().Section(Sec.TrustBoundaries).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Trust boundaries"));
                        foreach (var b in project.TrustBoundaries)
                            s.Item().Text($"• {b.Name}: {b.Description}");
                    });

                    if (c4OverviewPng is { Length: > 0 })
                    {
                        col.Item().Section(Sec.C4Visual).Column(s =>
                        {
                            s.Spacing(10);
                            s.Item().Element(SectionTitle("C4-overzicht (visualisatie)"));
                            s.Item().Image(c4OverviewPng).FitArea();
                        });
                    }

                    col.Item().Section(Sec.C4Scope).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("C4 threatmodel-scope"));
                        s.Item().Text(
                            "Het C4-model (Simon Brown) heeft vier zoomniveaus: van context tot code. In DesignGuard vullen " +
                            "we dit los van het architectuurcanvas; het helpt om dreigingen te koppelen aan benoemde onderdelen.");
                        s.Item().Text(
                            "Koppeling naar dreigingen: gebruik exact dezelfde naam in ‘getroffen componenten’ van een open dreiging " +
                            "als bij het C4-element hieronder. De kolom ‘open dreig.’ telt die matches.");

                        foreach (var lvl in new[]
                                 {
                                     C4Level.Context, C4Level.Container, C4Level.Component, C4Level.Code
                                 })
                        {
                            s.Item().PaddingLeft(6).Text($"{C4LevelFormatting.ShortLabel(lvl)} — {C4LevelFormatting.LevelScopeExplanation(lvl)}")
                                .FontSize(9.5f).FontColor(PdfPalette.Muted);
                        }

                        if (project.C4Elements.Count == 0)
                        {
                            s.Item().Text("— Geen C4-elementen vastgelegd in dit dossier.").FontColor(Colors.Grey.Medium);
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

                                s.Item().Text(C4LevelFormatting.ShortLabel(lvl)).SemiBold().FontSize(11).FontColor(PdfPalette.Primary);
                                foreach (var el in els)
                                {
                                    var openHits = C4ExportPresentation.CountOpenThreatNameMatches(el, threats);
                                    var parentHint = C4ExportPresentation.FormatC4ParentHintPdf(el, idToName);
                                    var tech = string.IsNullOrWhiteSpace(el.Technology) ? "" : $" — Tech/notitie: {el.Technology}";
                                    s.Item().PaddingLeft(10).Text($"{el.Name} (id {el.Id}){parentHint}{tech} — open dreig. met naam-match: {openHits}")
                                        .SemiBold().FontColor(PdfPalette.Muted);
                                    if (!string.IsNullOrWhiteSpace(el.Description))
                                        s.Item().PaddingLeft(18).Text(el.Description);
                                }
                            }
                        }
                    });

                    col.Item().Section(Sec.Threats).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Dreigingen (selectie)"));
                        foreach (var t in threats.OrderBy(x => x.Title).Take(PdfThreatListMax))
                        {
                            s.Item().Text($"{t.Title} — {t.StrideCategory}, {t.Severity}, {t.Status}").SemiBold().FontColor(PdfPalette.Primary);
                            s.Item().PaddingLeft(12).Text(t.Description);
                            if (t.StatusChangedAtUtc is { } aud)
                            {
                                var note = string.IsNullOrWhiteSpace(t.StatusChangeNote) ? "" : $" — {t.StatusChangeNote}";
                                s.Item().PaddingLeft(12).Text($"Audit: {aud:yyyy-MM-dd HH:mm} UTC — {t.StatusChangedBy}{note}")
                                    .FontSize(9.5f).FontColor(PdfPalette.Muted);
                            }
                        }

                        if (threats.Count > PdfThreatListMax)
                            s.Item().Text($"... en {threats.Count - PdfThreatListMax} extra (Markdown/JSON-export).");
                    });

                    col.Item().Section(Sec.Requirements).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Security-eisen (selectie)"));
                        foreach (var r in requirements.OrderBy(x => x.Category).ThenBy(x => x.Title).Take(PdfRequirementListMax))
                        {
                            s.Item().Text($"{r.Title} [{r.Category}] — {r.Priority}, {r.Status}").SemiBold().FontColor(PdfPalette.Primary);
                            s.Item().PaddingLeft(12).Text(r.PlainExplanation);
                            if (r.StatusChangedAtUtc is { } rAud)
                            {
                                var rNote = string.IsNullOrWhiteSpace(r.StatusChangeNote) ? "" : $" — {r.StatusChangeNote}";
                                s.Item().PaddingLeft(12).Text($"Audit: {rAud:yyyy-MM-dd HH:mm} UTC — {r.StatusChangedBy}{rNote}")
                                    .FontSize(9.5f).FontColor(PdfPalette.Muted);
                            }

                            if (!string.IsNullOrWhiteSpace(r.SourceAttribution.KnowledgePackId))
                                s.Item().PaddingLeft(12).Text(
                                    $"Bronspoor: pack {r.SourceAttribution.KnowledgePackDisplayLabel} ({r.SourceAttribution.KnowledgePackVersionLabel}), " +
                                    $"items: {string.Join(", ", r.SourceAttribution.GuidanceItemIds)} — {r.SourceAttribution.Nature}")
                                    .FontColor(PdfPalette.Muted);
                        }

                        if (requirements.Count > PdfRequirementListMax)
                            s.Item().Text($"... en {requirements.Count - PdfRequirementListMax} extra (Markdown/JSON-export).");
                    });

                    col.Item().Section(Sec.Traceability).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Traceability (trigger-sleutels)"));
                        s.Item().Text(
                                "Korte koppeling ontwerp → dreiging/eis. Uitgebreide toelichting: tab Traceability en Markdown-export.")
                            .FontSize(9.5f).FontColor(PdfPalette.Muted);
                        foreach (var t in threats.OrderBy(x => x.StrideCategory).ThenBy(x => x.Title).Take(PdfTraceabilityTriggerMax))
                        {
                            var keys = t.TriggerKeys.Count > 0 ? string.Join(", ", t.TriggerKeys) : "—";
                            s.Item().PaddingLeft(8).Text($"Dreiging — {t.Title}: {keys}").FontSize(9.5f);
                        }

                        if (threats.Count > PdfTraceabilityTriggerMax)
                            s.Item().Text($"... {threats.Count - PdfTraceabilityTriggerMax} extra dreigingen.")
                                .FontColor(Colors.Grey.Medium);

                        foreach (var r in requirements.OrderBy(x => x.Category).ThenBy(x => x.Title).Take(PdfTraceabilityTriggerMax))
                        {
                            var keys = r.TriggerKeys.Count > 0 ? string.Join(", ", r.TriggerKeys) : "—";
                            s.Item().PaddingLeft(8).Text($"Eis — {r.Title}: {keys}").FontSize(9.5f);
                        }

                        if (requirements.Count > PdfTraceabilityTriggerMax)
                            s.Item().Text($"... {requirements.Count - PdfTraceabilityTriggerMax} extra eisen.")
                                .FontColor(Colors.Grey.Medium);
                    });

                    col.Item().Section(Sec.Controls).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Controls"));
                        foreach (var c in project.Controls)
                            s.Item().Text($"• {c.Title}: {c.Description}");
                    });

                    col.Item().Section(Sec.Decisions).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Beslissingen en aannames"));
                        foreach (var n in project.DesignNotes.OrderBy(x => x.Kind).ThenBy(x => x.Title))
                            s.Item().Text($"[{n.Kind}] {n.Title}: {n.Description}");
                    });

                    col.Item().Section(Sec.Packs).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Knowledge packs (actief geladen)"));
                        foreach (var p in _packs.LoadedPacks)
                            s.Item().Text($"• {p.Dto.DisplayLabel} — v{p.Dto.VersionLabel} — {p.Dto.SourceName}");
                    });

                    col.Item().Section(Sec.OpenIssues).Column(s =>
                    {
                        s.Spacing(10);
                        s.Item().Element(SectionTitle("Open issues"));
                        s.Item().Text(string.IsNullOrWhiteSpace(project.OpenIssuesSummary)
                            ? "—"
                            : project.OpenIssuesSummary);
                    });
                });
            });
        }).GeneratePdf();
    }

    private static Action<IContainer> SectionTitle(string title) => container =>
    {
        container.PaddingTop(4).PaddingBottom(8).Column(c =>
        {
            c.Item().Text(title).FontSize(13).Bold().FontColor(PdfPalette.Primary);
            c.Item().LineHorizontal(2).LineColor(PdfPalette.Accent);
        });
    };

    private static void AddTocLine(ColumnDescriptor col, string sectionId, string title)
    {
        col.Item().PaddingVertical(4).Row(row =>
        {
            row.RelativeItem().SectionLink(sectionId).Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(11).FontColor(PdfPalette.Link).SemiBold());
                text.Span(title);
            });
            row.ConstantItem(36).AlignRight().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(11).FontColor(PdfPalette.Muted));
                text.BeginPageNumberOfSection(sectionId);
            });
        });
    }
}

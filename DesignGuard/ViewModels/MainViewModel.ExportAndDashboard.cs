// Export, traceability, filters en dashboard-tellingen.
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Export;
using DesignGuard.Models;
using DesignGuard.Security;
using DesignGuard.Services;
using Microsoft.Win32;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    partial void OnLastExportedFilePathChanged(string? value) =>
        OpenLastExportLocationCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanOpenLastExportLocation))]
    private void OpenLastExportLocation()
    {
        if (string.IsNullOrWhiteSpace(LastExportedFilePath)) return;
        try
        {
            if (File.Exists(LastExportedFilePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{LastExportedFilePath}\"",
                    UseShellExecute = true
                });
                return;
            }

            var dir = Path.GetDirectoryName(LastExportedFilePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Locatie openen mislukt: {ex.Message}";
        }
    }

    private bool CanOpenLastExportLocation() => !string.IsNullOrWhiteSpace(LastExportedFilePath);

    private void RefreshExportPreview()
    {
        try
        {
            var m = BuildModelFromEditor();
            ExportPreview = _export.ToMarkdown(m, Threats.ToList(), Requirements.ToList());
        }
        catch (Exception ex)
        {
            ExportPreview = $"Exportvoorbeeld mislukt: {ex.Message}";
        }
    }

    private void RefreshTraceability()
    {
        try
        {
            var m = BuildModelFromEditor();
            TraceabilityText = _traceability.BuildTraceabilitySummary(m);
        }
        catch
        {
            TraceabilityText = "Kon traceability niet opbouwen.";
        }
    }

    private void RefreshFilters()
    {
        FilteredThreats = new ObservableCollection<ThreatModel>(
            EditorListFilter.FilterAndSortThreats(Threats, ThreatFilterText, ThreatSort, ThreatQuickFilter));
        FilteredRequirements = new ObservableCollection<RequirementModel>(
            EditorListFilter.FilterAndSortRequirements(Requirements, RequirementFilterText, RequirementSort,
                RequirementQuickFilter));
        FilteredFindings = new ObservableCollection<PentestFindingModel>(
            EditorListFilter.FilterAndSortFindings(Findings, FindingFilterText, FindingSort, FindingQuickFilter));
    }

    private void UpdateDashboard()
    {
        var (o, m, orc, ir) = DashboardMetrics.Compute(Threats, Requirements);
        OpenThreatCount = o;
        MitigatedThreatCount = m;
        OpenRequirementCount = orc;
        ImplementedRequirementCount = ir;
        RefreshC4ThreatLinkCounts();
        try
        {
            var model = BuildModelFromEditor();
            ValidationSummaryText = FormatValidation(_designValidation.Validate(model));
        }
        catch (Exception ex)
        {
            ValidationSummaryText = $"Validatie kon niet worden uitgevoerd: {ex.Message}";
        }

        RefreshPlaybook();
        RefreshRiskAnalysis();
        CoverageSummaryText = CoverageCatalog.Summary(CoverageItems);
    }

    private static string FormatValidation(IReadOnlyList<DesignValidationFinding> findings) =>
        string.Join(Environment.NewLine, findings.Select(f => f.Severity switch
        {
            DesignValidationSeverity.Error => $"[Fout] {f.Code}: {f.Message}",
            DesignValidationSeverity.Warning => $"[Waarschuwing] {f.Code}: {f.Message}",
            _ => $"[Info] {f.Code}: {f.Message}"
        }));

    [RelayCommand]
    private void ExportMarkdown()
    {
        try
        {
            var m = BuildModelFromEditor();
            var md = _export.ToMarkdown(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "Markdown (*.md)|*.md|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.md"
            };
            if (!ShowModalSaveDialog(dlg)) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, md);
            LastExportedFilePath = path;
            StatusMessage = $"Markdown geëxporteerd: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportPlainText()
    {
        try
        {
            var m = BuildModelFromEditor();
            var txt = _export.ToPlainText(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "Text (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.txt"
            };
            if (!ShowModalSaveDialog(dlg)) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, txt);
            LastExportedFilePath = path;
            StatusMessage = $"Tekst geëxporteerd: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportHtml()
    {
        try
        {
            var m = BuildModelFromEditor();
            var html = _export.ToHtml(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "HTML (*.html)|*.html|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.html"
            };
            if (!ShowModalSaveDialog(dlg)) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, html);
            LastExportedFilePath = path;
            StatusMessage = $"HTML geëxporteerd: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportPrintFriendlyHtml()
    {
        try
        {
            var m = BuildModelFromEditor();
            var html = _export.ToPrintFriendlyHtml(m, Threats.ToList(), Requirements.ToList(), DateTime.UtcNow);
            var dlg = new SaveFileDialog
            {
                Filter = "HTML (*.html)|*.html|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard-print.html"
            };
            if (!ShowModalSaveDialog(dlg)) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, html);
            LastExportedFilePath = path;
            StatusMessage = $"Print-HTML geëxporteerd: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportPdf()
    {
        try
        {
            var m = BuildModelFromEditor();
            var threats = Threats.ToList();
            var reqs = Requirements.ToList();
            var dlg = new SaveFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.pdf"
            };
            if (!ShowModalSaveDialog(dlg)) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            var disp = Application.Current?.Dispatcher;
            if (disp == null)
            {
                StatusMessage = "PDF-export mislukt: geen UI-context.";
                return;
            }

            IsBusy = true;
            BusyMessage = "PDF opbouwen (Mermaid + C4 + rapport)…";
            // WebView2-capture alleen op de UI-thread.
            var pngB = await disp.InvokeAsync(async () =>
            {
                RefreshDiagram();
                return await _mermaidRasterizer.RenderToPngAsync(MermaidCode ?? string.Empty).ConfigureAwait(true);
            }).Task.Unwrap().ConfigureAwait(true);

            IReadOnlyList<PdfC4MermaidBandImage> c4Mermaid;
            if (m.C4Elements.Count == 0)
            {
                c4Mermaid = Array.Empty<PdfC4MermaidBandImage>();
            }
            else
            {
                var bands = new List<PdfC4MermaidBandImage>(4);
                var bandRows = new (C4MermaidBand band, string caption)[]
                {
                    (C4MermaidBand.Context, "C1 - Context"),
                    (C4MermaidBand.Container, "C2 - Containers"),
                    (C4MermaidBand.Component, "C3 - Components"),
                    (C4MermaidBand.Code, "C4 - Code")
                };
                foreach (var (band, caption) in bandRows)
                {
                    var code = _c4MermaidBuilder.Build(band, m);
                    try
                    {
                        var png = await disp.InvokeAsync(async () =>
                                await _mermaidRasterizer.RenderToPngAsync(code).ConfigureAwait(true))
                            .Task.Unwrap()
                            .ConfigureAwait(true);
                        bands.Add(new PdfC4MermaidBandImage(caption, png));
                    }
                    catch
                    {
                        bands.Add(new PdfC4MermaidBandImage(caption, Array.Empty<byte>()));
                    }
                }

                c4Mermaid = bands;
            }

            var pdf = await Task.Run(() => _pdfReport.BuildSecurityDesignReport(m, threats, reqs, pngB, c4Mermaid))
                .ConfigureAwait(true);

            await File.WriteAllBytesAsync(path, pdf).ConfigureAwait(true);
            LastExportedFilePath = path;
            StatusMessage = $"PDF geëxporteerd: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"PDF-export mislukt: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }

    [RelayCommand]
    private void ExportStructuredJson()
    {
        try
        {
            var m = BuildModelFromEditor();
            var json = _export.ToStructuredJson(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.json"
            };
            if (!ShowModalSaveDialog(dlg)) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, json);
            LastExportedFilePath = path;
            StatusMessage = $"JSON geëxporteerd: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "project" : name.Trim();
    }

    /// <summary>Modaal aan hoofdvenster koppelen (betrouwbaarder pad/OK op Windows).</summary>
    private static bool ShowModalSaveDialog(SaveFileDialog dlg) =>
        dlg.ShowDialog(Application.Current?.MainWindow) == true;
}

// Export, traceability, filters en dashboard-tellingen.
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;
using DesignGuard.Security;
using DesignGuard.Services;
using Microsoft.Win32;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
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
            EditorListFilter.FilterAndSortThreats(Threats, ThreatFilterText, ThreatSort));
        FilteredRequirements = new ObservableCollection<RequirementModel>(
            EditorListFilter.FilterAndSortRequirements(Requirements, RequirementFilterText, RequirementSort));
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
            BusyMessage = "PDF opbouwen (diagram + rapport)…";
            // WPF-diagramraster alleen op de UI-thread (RenderTargetBitmap / visuele elementen).
            var pngB = await disp.InvokeAsync(() => _diagramRasterizer.RenderPng(m));
            var pdf = await Task.Run(() => _pdfReport.BuildSecurityDesignReport(m, threats, reqs, pngB))
                .ConfigureAwait(true);

            await File.WriteAllBytesAsync(path, pdf).ConfigureAwait(true);
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

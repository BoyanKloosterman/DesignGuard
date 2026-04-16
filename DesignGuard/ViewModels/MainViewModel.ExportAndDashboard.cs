// Export, traceability, filters en dashboard-tellingen.
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;
using DesignGuard.Security;
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
        IEnumerable<ThreatModel> tq = Threats;
        if (!string.IsNullOrWhiteSpace(ThreatFilterText))
        {
            var f = ThreatFilterText.Trim();
            tq = tq.Where(t =>
                t.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                t.StrideCategory.ToString().Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        tq = ThreatSort switch
        {
            "Status" => tq.OrderBy(t => t.Status).ThenBy(t => t.Title),
            "Category" => tq.OrderBy(t => t.StrideCategory).ThenBy(t => t.Title),
            _ => tq.OrderByDescending(t => t.Severity).ThenBy(t => t.Title)
        };

        FilteredThreats = new ObservableCollection<ThreatModel>(tq);

        IEnumerable<RequirementModel> rq = Requirements;
        if (!string.IsNullOrWhiteSpace(RequirementFilterText))
        {
            var f = RequirementFilterText.Trim();
            rq = rq.Where(r =>
                r.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                r.PlainExplanation.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        rq = RequirementSort switch
        {
            "Status" => rq.OrderBy(r => r.Status).ThenBy(r => r.Title),
            "Category" => rq.OrderBy(r => r.Category).ThenBy(r => r.Title),
            _ => rq.OrderByDescending(r => r.Priority).ThenBy(r => r.Title)
        };

        FilteredRequirements = new ObservableCollection<RequirementModel>(rq);
    }

    private void UpdateDashboard()
    {
        OpenThreatCount = Threats.Count(t => t.Status == ThreatStatus.Open);
        MitigatedThreatCount =
            Threats.Count(t => t.Status is ThreatStatus.Mitigated or ThreatStatus.Accepted);
        OpenRequirementCount = Requirements.Count(r =>
            r.Status is RequirementStatus.Proposed or RequirementStatus.Accepted);
        ImplementedRequirementCount = Requirements.Count(r => r.Status == RequirementStatus.Implemented);
    }

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
            if (dlg.ShowDialog() != true) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, md);
            StatusMessage = "Markdown geëxporteerd.";
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
            if (dlg.ShowDialog() != true) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, txt);
            StatusMessage = "Tekst geëxporteerd.";
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
            if (dlg.ShowDialog() != true) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, html);
            StatusMessage = "HTML geëxporteerd.";
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
            if (dlg.ShowDialog() != true) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, html);
            StatusMessage = "Print-HTML geëxporteerd.";
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
            if (dlg.ShowDialog() != true) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            IsBusy = true;
            BusyMessage = "PDF opbouwen (diagram + rapport)…";
            var (_, pdf) = await Task.Run(() =>
            {
                var pngB = _diagramRasterizer.RenderPng(m);
                var pdfB = _pdfReport.BuildSecurityDesignReport(m, threats, reqs, pngB);
                return (pngB, pdfB);
            }).ConfigureAwait(true);

            await File.WriteAllBytesAsync(path, pdf);
            StatusMessage = "PDF geëxporteerd.";
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
            if (dlg.ShowDialog() != true) return;
            if (!SafeExportPath.TryGetSafeWritePath(dlg.FileName, out var path, out var err))
            {
                StatusMessage = err ?? "Export geannuleerd.";
                return;
            }

            File.WriteAllText(path, json);
            StatusMessage = "JSON geëxporteerd.";
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
}

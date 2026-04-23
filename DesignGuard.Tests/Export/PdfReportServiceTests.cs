using System.IO;
using DesignGuard.Export;
using DesignGuard.Knowledge;
using DesignGuard.Models;
using DesignGuard.Settings;
using QuestPDF.Infrastructure;
using Xunit;

namespace DesignGuard.Tests.Export;

public sealed class PdfReportServiceTests
{
    static PdfReportServiceTests() => QuestPDF.Settings.License = LicenseType.Community;

    [Fact]
    public void BuildSecurityDesignReport_leeg_project_geeft_pdf_bytes()
    {
        var dir = Path.Combine(Path.GetTempPath(), "dg-pdftest-" + Guid.NewGuid().ToString("N"));
        try
        {
            var settings = new UserSettingsService(dir);
            var packs = new KnowledgePackService(settings);
            var svc = new PdfReportService(packs);

            var project = new ProjectModel
            {
                Name = "Leeg",
                SystemName = "Sys"
            };

            var bytes = svc.BuildSecurityDesignReport(
                project,
                new List<ThreatModel>(),
                new List<RequirementModel>(),
                diagramPng: null,
                c4MermaidBands: null);

            Assert.NotNull(bytes);
            Assert.True(bytes.Length > 1500, "PDF zou substantieel moeten zijn.");
            Assert.Equal(0x25, bytes[0]); // '%' PDF header
            Assert.Equal(0x50, bytes[1]);
            Assert.Equal(0x44, bytes[2]);
            Assert.Equal(0x46, bytes[3]);
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // temp
            }
        }
    }
}

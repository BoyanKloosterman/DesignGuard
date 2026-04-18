using DesignGuard.Export;
using DesignGuard.Models;
using Xunit;

namespace DesignGuard.Tests.Export;

public sealed class ExportServiceTests
{
    private readonly ExportService _sut = new();

    [Fact]
    public void ToMarkdown_bevat_projectnaam_en_secties()
    {
        var p = new ProjectModel
        {
            Name = "Testproject",
            Description = "Omschrijving",
            SystemName = "Sys"
        };

        var md = _sut.ToMarkdown(p, [], []);

        Assert.Contains("# Testproject", md);
        Assert.Contains("## Projectoverzicht", md);
        Assert.Contains("## Systeemcontext", md);
        Assert.Contains("Omschrijving", md);
    }

    [Fact]
    public void ToPlainText_bevat_naam()
    {
        var p = new ProjectModel { Name = "P1" };
        var txt = _sut.ToPlainText(p, [], []);
        Assert.Contains("P1", txt);
    }

    [Fact]
    public void ToStructuredJson_is_geldige_json_met_naam()
    {
        var p = new ProjectModel { Name = "JsonProj" };
        var json = _sut.ToStructuredJson(p, [], []);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("JsonProj", doc.RootElement.GetProperty("project").GetProperty("name").GetString());
    }
}

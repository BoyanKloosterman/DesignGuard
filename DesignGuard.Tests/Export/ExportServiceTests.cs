using DesignGuard.Export;
using DesignGuard.Models;
using DesignGuard.Services;
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
            SystemName = "Sys",
            AssessmentGoal = "Grey-box webapp",
            AssessmentTestType = AssessmentTestType.GreyBox,
            ScopeIn = "Testomgeving"
        };

        var md = _sut.ToMarkdown(p, [], []);

        Assert.Contains("# Testproject", md);
        Assert.Contains("## Projectoverzicht", md);
        Assert.Contains("## Systeemcontext", md);
        Assert.Contains("## Kick-off en scope (pentest)", md);
        Assert.Contains("GreyBox", md);
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

    [Fact]
    public void ToMarkdown_bevat_normatieve_appendix_bij_eisen_met_tags()
    {
        var p = new ProjectModel { Name = "N" };
        var req = new RequirementModel
        {
            Title = "T",
            Category = "C",
            SourceTags = new List<string> { "OWASP" }
        };

        var md = _sut.ToMarkdown(p, [], new[] { req });
        Assert.Contains("Normatieve dekking", md);
        Assert.Contains("OWASP", md);
    }

    [Fact]
    public void ToMarkdown_bevat_risicoregister()
    {
        var p = new ProjectModel { Name = "N" };
        var md = _sut.ToMarkdown(p, [new ThreatModel { Title = "XSS", Likelihood = 3, Impact = 4 }], []);
        Assert.Contains("Risicoanalyse", md);
        Assert.Contains("K3 × I4 = 12 (Hoog)", md);
    }

    [Fact]
    public void ToMarkdown_bevat_kickoff_extra_velden_en_bevinding()
    {
        var p = new ProjectModel
        {
            Name = "P",
            AssessmentGoal = "Grey-box",
            AssessmentContact = "Sec-eigenaar",
            AssessmentWindow = "week 16",
            AssessmentEnvironment = "test",
            AssessmentAccounts = "shop-user",
            AssessmentLimitations = "geen DoS",
            Findings =
            [
                new PentestFindingModel
                {
                    Title = "IDOR admin",
                    WstgCategory = "Autorisatie",
                    Likelihood = 4,
                    Impact = 5,
                    Status = FindingStatus.Open,
                    EvidenceNotes = "HTTP 200 op andere tenant"
                }
            ]
        };

        var md = _sut.ToMarkdown(p, [], []);
        Assert.Contains("Kick-off en scope (pentest)", md);
        Assert.Contains("Sec-eigenaar", md);
        Assert.Contains("week 16", md);
        Assert.Contains("Bevindingenregister", md);
        Assert.Contains("IDOR admin", md);
        Assert.Contains("K4 × I5 = 20 (Kritiek)", md);
    }

    [Fact]
    public void ToMarkdown_bevat_niet_getest_en_rest_risico()
    {
        var p = new ProjectModel
        {
            Name = "P",
            AssessmentResidualNotes = "IDOR open tot fix.",
            CoverageItems = CoverageCatalog.Merge(null),
            Findings =
            [
                new PentestFindingModel
                {
                    Title = "IDOR admin",
                    Likelihood = 4,
                    Impact = 5,
                    Status = FindingStatus.Open
                }
            ]
        };
        p.CoverageItems.First(c => c.Id == "cov-api").Status = CoverageStatus.Blocked;
        p.CoverageItems.First(c => c.Id == "cov-api").Notes = "WAF";
        p.TestBlockers.Add(new TestBlockerModel { Title = "WAF", Reason = "rate-limit" });

        var md = _sut.ToMarkdown(p, [], []);
        Assert.Contains("## Testdekking", md);
        Assert.Contains("## Niet getest", md);
        Assert.Contains("WAF", md);
        Assert.Contains("## Rest-risico", md);
        Assert.Contains("IDOR open tot fix.", md);
        Assert.Contains("IDOR admin", md);
    }
}

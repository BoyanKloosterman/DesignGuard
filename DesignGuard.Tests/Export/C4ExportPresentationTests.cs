using DesignGuard.Export;
using DesignGuard.Models;
using Xunit;

namespace DesignGuard.Tests.Export;

public sealed class C4ExportPresentationTests
{
    [Fact]
    public void CountOpenThreatMatches_telt_case_insensitive_en_trim()
    {
        var threats = new List<ThreatModel>
        {
            new()
            {
                Status = ThreatStatus.Open,
                AffectedComponents = new List<string> { "  API-gateway ", "anders" }
            },
            new()
            {
                Status = ThreatStatus.Mitigated,
                AffectedComponents = new List<string> { "API-gateway" }
            }
        };

        Assert.Equal(1, C4ExportPresentation.CountOpenThreatMatchesForComponentName("api-gateway", threats));
        Assert.Equal(0, C4ExportPresentation.CountOpenThreatMatchesForComponentName("", threats));
        Assert.Equal(0, C4ExportPresentation.CountOpenThreatMatchesForComponentName("ontbreekt", threats));
    }

    [Fact]
    public void CountOpenThreatNameMatches_delegeert_naar_componentnaam()
    {
        var el = new C4ElementModel { Name = "Svc" };
        var threats = new List<ThreatModel>
        {
            new() { Status = ThreatStatus.Open, AffectedComponents = new List<string> { "Svc" } }
        };
        Assert.Equal(1, C4ExportPresentation.CountOpenThreatNameMatches(el, threats));
    }
}

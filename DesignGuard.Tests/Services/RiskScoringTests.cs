using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class RiskScoringTests
{
    [Theory]
    [InlineData(1, 1, 1, RiskLevel.Low)]
    [InlineData(2, 2, 4, RiskLevel.Low)]
    [InlineData(3, 3, 9, RiskLevel.Medium)]
    [InlineData(4, 4, 16, RiskLevel.High)]
    [InlineData(5, 5, 25, RiskLevel.Critical)]
    [InlineData(0, 3, 0, RiskLevel.Unspecified)]
    public void Score_en_level(int kans, int impact, int score, RiskLevel level)
    {
        Assert.Equal(score, RiskScoring.Score(kans, impact));
        Assert.Equal(level, RiskScoring.Level(score));
    }

    [Fact]
    public void EnsureScores_vult_oude_ernst_zonder_kxI()
    {
        var t = new ThreatModel { Severity = SeverityEstimate.High };
        RiskScoring.EnsureScores(t);
        Assert.Equal(4, t.Likelihood);
        Assert.Equal(4, t.Impact);
        Assert.Equal(16, t.RiskScore);
        Assert.Equal(RiskLevel.High, t.RiskLevel);
        Assert.Equal(SeverityEstimate.High, t.Severity);
    }

    [Fact]
    public void EnsureScores_laag_rondrit()
    {
        var t = new ThreatModel { Severity = SeverityEstimate.Low };
        RiskScoring.EnsureScores(t);
        Assert.Equal(2, t.Likelihood);
        Assert.Equal(2, t.Impact);
        Assert.Equal(RiskLevel.Low, t.RiskLevel);
        Assert.Equal(SeverityEstimate.Low, t.Severity);
    }

    [Fact]
    public void EnsureScores_laat_bestaande_scores_staan()
    {
        var t = new ThreatModel { Likelihood = 5, Impact = 2, Severity = SeverityEstimate.Low };
        RiskScoring.EnsureScores(t);
        Assert.Equal(5, t.Likelihood);
        Assert.Equal(2, t.Impact);
        Assert.Equal(10, t.RiskScore);
        Assert.Equal(SeverityEstimate.High, t.Severity);
    }

    [Fact]
    public void FormatSummary_toont_kxI()
    {
        var t = new ThreatModel { Likelihood = 3, Impact = 4 };
        Assert.Equal("K3 × I4 = 12 (Hoog)", RiskScoring.FormatSummary(t));
        Assert.Equal("K3 × I4 = 12 (Hoog)", RiskScoring.FormatSummary(3, 4));
    }

    [Fact]
    public void Finding_kxI_volgt_zelfde_schaal()
    {
        var f = new PentestFindingModel { Likelihood = 4, Impact = 5, Status = FindingStatus.Open };
        Assert.Equal(20, f.RiskScore);
        Assert.Equal(RiskLevel.Critical, f.RiskLevel);
        Assert.Equal("K4 × I5 = 20 (Kritiek)", f.RiskSummary);
        Assert.True(f.CountsInHeatmap);
        f.Status = FindingStatus.Remediated;
        Assert.False(f.CountsInHeatmap);
    }
}

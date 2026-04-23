using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class EditorListFilterTests
{
    [Fact]
    public void FilterAndSortThreats_tekstfilter_op_titel()
    {
        var a = new ThreatModel { Title = "Alpha leak", Description = "x", StrideCategory = StrideCategory.InformationDisclosure };
        var b = new ThreatModel { Title = "Beta", Description = "leak in body", StrideCategory = StrideCategory.Spoofing };
        var list = new[] { a, b };

        var r = EditorListFilter.FilterAndSortThreats(list, "leak", "Severity");
        Assert.Equal(2, r.Count);
        Assert.Contains(a, r);
        Assert.Contains(b, r);
    }

    [Fact]
    public void FilterAndSortThreats_sorteert_op_Severity_desc_standaard()
    {
        var low = new ThreatModel { Title = "L", Severity = SeverityEstimate.Low };
        var high = new ThreatModel { Title = "H", Severity = SeverityEstimate.High };
        var r = EditorListFilter.FilterAndSortThreats(new[] { low, high }, null, "Severity");
        Assert.Equal(high, r[0]);
        Assert.Equal(low, r[1]);
    }

    [Fact]
    public void FilterAndSortThreats_sorteert_op_Status()
    {
        var open = new ThreatModel { Title = "A", Status = ThreatStatus.Open };
        var mit = new ThreatModel { Title = "B", Status = ThreatStatus.Mitigated };
        var r = EditorListFilter.FilterAndSortThreats(new[] { mit, open }, null, "Status");
        Assert.Equal(open, r[0]);
        Assert.Equal(mit, r[1]);
    }

    [Fact]
    public void FilterAndSortRequirements_sorteert_op_Priority_desc_standaard()
    {
        var lo = new RequirementModel { Title = "x", Priority = RequirementPriority.Low };
        var hi = new RequirementModel { Title = "y", Priority = RequirementPriority.High };
        var r = EditorListFilter.FilterAndSortRequirements(new[] { lo, hi }, null, "Priority");
        Assert.Equal(hi, r[0]);
        Assert.Equal(lo, r[1]);
    }

    [Fact]
    public void FilterAndSortRequirements_filter_op_categorie()
    {
        var a = new RequirementModel { Title = "t1", Category = "Auth" };
        var b = new RequirementModel { Title = "t2", Category = "Netwerk" };
        var r = EditorListFilter.FilterAndSortRequirements(new[] { a, b }, "auth", "Category");
        Assert.Single(r);
        Assert.Equal(a, r[0]);
    }

    [Fact]
    public void FilterAndSortThreats_quickfilter_alleen_open()
    {
        var open = new ThreatModel { Title = "A", Status = ThreatStatus.Open };
        var mit = new ThreatModel { Title = "B", Status = ThreatStatus.Mitigated };
        var r = EditorListFilter.FilterAndSortThreats(new[] { mit, open }, null, "Severity",
            EditorListFilter.QuickFilterAlleenOpen);
        Assert.Single(r);
        Assert.Equal(open, r[0]);
    }

    [Fact]
    public void FilterAndSortThreats_quickfilter_alleen_hoog()
    {
        var low = new ThreatModel { Title = "L", Severity = SeverityEstimate.Low };
        var high = new ThreatModel { Title = "H", Severity = SeverityEstimate.High };
        var r = EditorListFilter.FilterAndSortThreats(new[] { low, high }, null, "Severity",
            EditorListFilter.QuickFilterAlleenHoog);
        Assert.Single(r);
        Assert.Equal(high, r[0]);
    }

    [Fact]
    public void FilterAndSortRequirements_quickfilter_alleen_open_status()
    {
        var open = new RequirementModel { Title = "a", Status = RequirementStatus.Proposed };
        var done = new RequirementModel { Title = "b", Status = RequirementStatus.Implemented };
        var r = EditorListFilter.FilterAndSortRequirements(new[] { done, open }, null, "Priority",
            EditorListFilter.ReqQuickFilterAlleenOpen);
        Assert.Single(r);
        Assert.Equal(open, r[0]);
    }

    [Fact]
    public void FilterAndSortRequirements_quickfilter_hoge_prioriteit()
    {
        var lo = new RequirementModel { Title = "x", Priority = RequirementPriority.Low };
        var hi = new RequirementModel { Title = "y", Priority = RequirementPriority.High };
        var r = EditorListFilter.FilterAndSortRequirements(new[] { lo, hi }, null, "Priority",
            EditorListFilter.ReqQuickFilterAlleenHoogPrio);
        Assert.Single(r);
        Assert.Equal(hi, r[0]);
    }
}

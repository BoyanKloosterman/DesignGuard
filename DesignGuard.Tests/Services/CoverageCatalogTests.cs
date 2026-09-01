using DesignGuard.Models;
using DesignGuard.Services;
using Xunit;

namespace DesignGuard.Tests.Services;

public sealed class CoverageCatalogTests
{
    [Fact]
    public void Merge_leeg_seed_acht_themas()
    {
        var list = CoverageCatalog.Merge(null);
        Assert.Equal(8, list.Count);
        Assert.Contains(list, c => c.Id == "cov-auth");
        Assert.Contains(list, c => c.Id == "cov-errors");
        Assert.All(list, c => Assert.Equal(CoverageStatus.NotStarted, c.Status));
    }

    [Fact]
    public void Merge_behoudt_status_en_notitie()
    {
        var existing = new[]
        {
            new CoverageItemModel
            {
                Id = "cov-api",
                Status = CoverageStatus.Blocked,
                Notes = "WAF"
            }
        };
        var list = CoverageCatalog.Merge(existing);
        var api = Assert.Single(list, c => c.Id == "cov-api");
        Assert.Equal(CoverageStatus.Blocked, api.Status);
        Assert.Equal("WAF", api.Notes);
        Assert.Equal("API", api.Title);
    }

    [Fact]
    public void Summary_telt_onderzocht_en_geblokkeerd()
    {
        var list = CoverageCatalog.Merge(null);
        list.First(c => c.Id == "cov-auth").Status = CoverageStatus.Tested;
        list.First(c => c.Id == "cov-api").Status = CoverageStatus.Blocked;
        Assert.Equal("Testdekking: 1/8 onderzocht, 1 geblokkeerd, 0 n.v.t.", CoverageCatalog.Summary(list));
        Assert.Single(CoverageCatalog.NotTested(list));
    }
}

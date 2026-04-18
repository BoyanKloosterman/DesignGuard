using System.IO;
using DesignGuard.Security;
using Xunit;

namespace DesignGuard.Tests.Security;

public sealed class SafeExportPathTests
{
    [Fact]
    public void TryGetSafeWritePath_leeg_pad_faalt()
    {
        var ok = SafeExportPath.TryGetSafeWritePath(null, out var path, out var err);
        Assert.False(ok);
        Assert.Equal("", path);
        Assert.NotNull(err);
    }

    [Fact]
    public void TryGetSafeWritePath_bestaande_map_slaagt()
    {
        var tmp = Path.GetTempPath();
        var file = Path.Combine(tmp, "designguard-test-export-" + Guid.NewGuid().ToString("N") + ".md");
        var ok = SafeExportPath.TryGetSafeWritePath(file, out var path, out var err);
        Assert.True(ok);
        Assert.Equal(Path.GetFullPath(file), path);
        Assert.Null(err);
    }

    [Fact]
    public void TryGetSafeWritePath_niet_bestaande_map_faalt()
    {
        var bogus = Path.Combine(Path.GetTempPath(), "niet-bestaand-map-xyz-123", "out.md");
        var ok = SafeExportPath.TryGetSafeWritePath(bogus, out _, out var err);
        Assert.False(ok);
        Assert.NotNull(err);
    }
}

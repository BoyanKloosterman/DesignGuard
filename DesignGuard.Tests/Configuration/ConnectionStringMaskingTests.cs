using DesignGuard.Configuration;
using Xunit;

namespace DesignGuard.Tests.Configuration;

public sealed class ConnectionStringMaskingTests
{
    [Fact]
    public void MaskMongoConnection_leeg_is_plaatshouder()
    {
        Assert.Equal("(niet ingesteld)", ConnectionStringMasking.MaskMongoConnection(null));
        Assert.Equal("(niet ingesteld)", ConnectionStringMasking.MaskMongoConnection("   "));
    }

    [Fact]
    public void MaskMongoConnection_verbergt_userinfo()
    {
        var raw = "mongodb://user:secret@localhost:27017/mydb";
        var masked = ConnectionStringMasking.MaskMongoConnection(raw);
        Assert.DoesNotContain("secret", masked);
        Assert.DoesNotContain("user", masked);
        Assert.Contains("localhost", masked);
    }

    [Fact]
    public void MaskMongoConnection_lange_string_wordt_ingekort()
    {
        var host = new string('x', 120);
        var raw = $"mongodb://u:p@{host}:27017/db";
        var masked = ConnectionStringMasking.MaskMongoConnection(raw);
        Assert.True(masked.Length < raw.Length);
        Assert.Contains("…", masked);
    }
}

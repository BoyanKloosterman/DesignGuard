using System.Globalization;
using System.Windows;
using DesignGuard.Converters;
using Xunit;

namespace DesignGuard.Tests.Converters;

public sealed class NotNullToVisibilityConverterTests
{
    private readonly NotNullToVisibilityConverter _sut = new();

    [Fact]
    public void Convert_niet_null_is_Visible()
    {
        var r = _sut.Convert(new object(), typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Visible, r);
    }

    [Fact]
    public void Convert_null_is_Collapsed()
    {
        var r = _sut.Convert(null, typeof(Visibility), null, CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Collapsed, r);
    }
}

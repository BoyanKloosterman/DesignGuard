using System.Globalization;
using System.Windows;
using DesignGuard.Converters;
using Xunit;

namespace DesignGuard.Tests.Converters;

public sealed class IntEqualsToVisibilityConverterTests
{
    private readonly IntEqualsToVisibilityConverter _sut = new();

    [Theory]
    [InlineData(0, "0", Visibility.Visible)]
    [InlineData(1, "0", Visibility.Collapsed)]
    [InlineData(2, "2", Visibility.Visible)]
    public void Convert_gelijke_index_en_parameter_is_Visible(int value, string parameter, Visibility expected)
    {
        var r = _sut.Convert(value, typeof(Visibility), parameter, CultureInfo.InvariantCulture);
        Assert.Equal(expected, r);
    }

    [Fact]
    public void Convert_ongeldige_parameter_is_Collapsed()
    {
        var r = _sut.Convert(1, typeof(Visibility), "geen_int", CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Collapsed, r);
    }

    [Fact]
    public void Convert_value_geen_int_is_Collapsed()
    {
        var r = _sut.Convert("1", typeof(Visibility), "1", CultureInfo.InvariantCulture);
        Assert.Equal(Visibility.Collapsed, r);
    }
}

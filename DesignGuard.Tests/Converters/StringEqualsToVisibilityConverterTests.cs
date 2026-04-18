using System.Globalization;
using System.Windows;
using DesignGuard.Converters;
using Xunit;

namespace DesignGuard.Tests.Converters;

public sealed class StringEqualsToVisibilityConverterTests
{
    private readonly StringEqualsToVisibilityConverter _sut = new();

    [Theory]
    [InlineData("Beginner", "Beginner", Visibility.Visible)]
    [InlineData("beginner", "Beginner", Visibility.Visible)]
    [InlineData("Advanced", "Beginner", Visibility.Collapsed)]
    [InlineData(null, "", Visibility.Visible)]
    public void Convert_verwachte_vergelijking(string? value, string parameter, Visibility expected)
    {
        var r = _sut.Convert(value, typeof(Visibility), parameter, CultureInfo.InvariantCulture);
        Assert.Equal(expected, r);
    }
}

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DesignGuard;

namespace DesignGuard.Converters;

/// <summary>Zichtbaar alleen als NavSection gelijk is aan de meegegeven <see cref="MainNavSection"/>.</summary>
public sealed class NavSectionEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not MainNavSection current) return Visibility.Collapsed;
        var target = Parse(parameter);
        return target == current ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static MainNavSection? Parse(object? parameter)
    {
        if (parameter is MainNavSection m) return m;
        if (parameter is string s && Enum.TryParse<MainNavSection>(s, out var e)) return e;
        return null;
    }
}

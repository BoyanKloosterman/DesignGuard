using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesignGuard.Converters;

/// <summary>Visible als stringwaarde gelijk is aan ConverterParameter.</summary>
public sealed class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var a = value?.ToString() ?? "";
        var b = parameter?.ToString() ?? "";
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

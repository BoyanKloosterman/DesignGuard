using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesignGuard.Converters;

public sealed class IntEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int i || parameter is not string s || !int.TryParse(s, out var target))
            return Visibility.Collapsed;
        return i == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

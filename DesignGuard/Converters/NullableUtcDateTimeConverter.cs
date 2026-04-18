using System.Globalization;
using System.Windows.Data;

namespace DesignGuard.Converters;

/// <summary>DateTime? (UTC) naar korte string of streepje.</summary>
public sealed class NullableUtcDateTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is DateTime dt
            ? dt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", culture)
            : "—";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

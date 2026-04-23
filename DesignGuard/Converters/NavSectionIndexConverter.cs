using System.Globalization;
using System.Windows.Data;
using DesignGuard;

namespace DesignGuard.Converters;

/// <summary>Sidebar ListBox SelectedIndex ↔ MainNavSection (zelfde volgorde als items).</summary>
public sealed class NavSectionIndexConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MainNavSection e) return (int)e;
        return 0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i && Enum.IsDefined(typeof(MainNavSection), i))
            return (MainNavSection)i;
        return MainNavSection.Dashboard;
    }
}

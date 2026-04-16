using System.Windows;

namespace DesignGuard.Theming;

/// <summary>Wisselt kleurpalet (licht/donker) zonder de rest van de styles te herladen.</summary>
public static class ThemeSwitcher
{
    private const string LightPack =
        "pack://application:,,,/DesignGuard;component/Themes/AppColors.Light.xaml";

    private const string DarkPack =
        "pack://application:,,,/DesignGuard;component/Themes/AppColors.Dark.xaml";

    public static void ApplyTheme(string? theme)
    {
        var app = Application.Current;
        if (app?.Resources.MergedDictionaries is not { Count: > 0 } merged)
            return;

        var dark = string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase);
        var uri = new Uri(dark ? DarkPack : LightPack, UriKind.Absolute);
        merged[0] = new ResourceDictionary { Source = uri };
    }
}

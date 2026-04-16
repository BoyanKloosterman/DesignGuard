namespace DesignGuard.Settings;

public sealed class UserAppSettings
{
    public List<string> DisabledPackIds { get; set; } = new();

    /// <summary>Waarschuwing als lastReviewed ouder is dan dit aantal dagen.</summary>
    public int PackStaleWarningDays { get; set; } = 365;

    public string ExportLastFolder { get; set; } = "";

    /// <summary>Light of Dark — zie ThemeSwitcher.</summary>
    public string Theme { get; set; } = "Light";

    /// <summary>Beginner = eenvoudigere uitleg; Advanced = volledige velden zichtbaar.</summary>
    public string DetailLevel { get; set; } = "Beginner";

    /// <summary>Comfortable of Compact — marges en sidebar.</summary>
    public string UiDensity { get; set; } = "Comfortable";
}

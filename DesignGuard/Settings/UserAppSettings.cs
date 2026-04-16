namespace DesignGuard.Settings;

public sealed class UserAppSettings
{
    public List<string> DisabledPackIds { get; set; } = new();

    /// <summary>Waarschuwing als lastReviewed ouder is dan dit aantal dagen.</summary>
    public int PackStaleWarningDays { get; set; } = 365;

    public string ExportLastFolder { get; set; } = "";

    public string Theme { get; set; } = "Light";
}

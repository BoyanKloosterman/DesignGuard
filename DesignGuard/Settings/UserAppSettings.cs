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

    /// <summary>Standaard naam bij vastleggen dreiging-status (audit).</summary>
    public string ReviewerDisplayName { get; set; } = "";

    /// <summary>HTTPS-manifest voor knowledge packs; leeg = alleen lokaal.</summary>
    public string KnowledgePackManifestUrl { get; set; } = "";

    /// <summary>Sync via netwerk toegestaan (expliciete opt-in).</summary>
    public bool KnowledgePackRemoteSyncEnabled { get; set; }

    /// <summary>Bij app-start manifest ophalen indien RemoteSyncEnabled en URL gezet.</summary>
    public bool KnowledgePackSyncOnStartup { get; set; }

    /// <summary>Optionele extra hostnaam (zelfde scheme HTTPS) naast manifest-host.</summary>
    public string KnowledgePackSyncTrustedHostExtra { get; set; } = "";
}

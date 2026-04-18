namespace DesignGuard.Services;

/// <summary>Standaardwaarden voor suggesties in bewerkbare comboboxen.</summary>
public static class DesignDropdownPresets
{
    public static readonly string[] ComponentTags =
    [
        "frontend", "backend", "api", "database", "cache", "queue", "storage",
        "gateway", "worker", "external", "admin", "identity", "mobile", "desktop",
        "edge", "service", "integration", "analytics", "loadbalancer", "cdn"
    ];

    public static readonly string[] ControlSourceTags =
    [
        "OWASP", "AVG", "GDPR-inspired", "NIS2-inspired", "Architecture", "ISO27001-inspired",
        "Privacy", "Logging", "Crypto", "Network", "IAM", "Operations", "Compliance"
    ];
}

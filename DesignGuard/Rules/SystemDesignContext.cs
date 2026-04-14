using DesignGuard.Models;

namespace DesignGuard.Rules;

/// <summary>
/// Snapshot van systeemkenmerken voor regels (geen I/O).
/// </summary>
public sealed class SystemDesignContext
{
    public SystemDesignContext(ProjectModel project)
    {
        Project = project;
    }

    public ProjectModel Project { get; }

    public bool HasDatabase =>
        Project.Components.Any(c => TagEquals(c.Tag, "database", "db", "datastore"));

    public bool HasExternalService =>
        Project.ExternalApis ||
        Project.Components.Any(c => TagEquals(c.Tag, "external", "third-party", "saas"));

    public bool HasApiLayer =>
        Project.SystemType is SystemType.Api or SystemType.WebApp or SystemType.MobileBackend ||
        Project.Components.Any(c => TagEquals(c.Tag, "api", "backend", "service"));

    public bool HasFrontend =>
        Project.SystemType == SystemType.WebApp ||
        Project.Components.Any(c => TagEquals(c.Tag, "frontend", "ui", "spa", "web"));

    public bool HasAdminSurface =>
        Project.HasAdmin ||
        Project.Components.Any(c =>
            c.Name.Contains("admin", StringComparison.OrdinalIgnoreCase) ||
            TagEquals(c.Tag, "admin"));

    private static bool TagEquals(string tag, params string[] allowed)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        return allowed.Any(a => tag.Equals(a, StringComparison.OrdinalIgnoreCase));
    }
}

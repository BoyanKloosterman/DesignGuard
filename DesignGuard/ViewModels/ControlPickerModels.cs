namespace DesignGuard.ViewModels;

/// <summary>Item voor dreiging-dropdown bij controls.</summary>
public sealed class ThreatPickItem(string id, string displayTitle)
{
    public string Id { get; } = id;
    public string DisplayTitle { get; } = displayTitle;
    public string Display => string.IsNullOrEmpty(Id) ? DisplayTitle : DisplayTitle;
}

/// <summary>Item voor control-bibliotheek-dropdown.</summary>
public sealed class LibraryPickItem(string id, string title)
{
    public string Id { get; } = id;
    public string Title { get; } = title;
    public string Display => string.IsNullOrEmpty(Id) ? Title : $"{Title} — {Id}";
}

/// <summary>Gekoppelde eis in controlrij (titel + id).</summary>
public sealed class ControlLinkedRequirementItem(string title, string id)
{
    public string Title { get; } = title;
    public string Id { get; } = id;
}

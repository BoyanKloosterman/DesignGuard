namespace DesignGuard.Models;

/// <summary>Regelgebaseerde modelleringssuggestie (geen AI).</summary>
public sealed class ModelingSuggestion
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Because { get; set; } = "";
}

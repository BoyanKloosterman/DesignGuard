using CommunityToolkit.Mvvm.ComponentModel;

namespace DesignGuard.ViewModels;

public partial class KnowledgePackToggleRow : ObservableObject
{
    private readonly Action<string, bool> _onEnabledChanged;

    public KnowledgePackToggleRow(
        string packId,
        string displayLabel,
        string versionLabel,
        string sourceName,
        bool isStale,
        bool isEnabled,
        Action<string, bool> onEnabledChanged)
    {
        PackId = packId;
        DisplayLabel = displayLabel;
        VersionLabel = versionLabel;
        SourceName = sourceName;
        IsStale = isStale;
        _onEnabledChanged = onEnabledChanged;
        _isEnabled = isEnabled;
    }

    public string PackId { get; }

    public string DisplayLabel { get; }

    public string VersionLabel { get; }

    public string SourceName { get; }

    public bool IsStale { get; }

    [ObservableProperty] private bool _isEnabled;

    partial void OnIsEnabledChanged(bool value) => _onEnabledChanged(PackId, value);
}

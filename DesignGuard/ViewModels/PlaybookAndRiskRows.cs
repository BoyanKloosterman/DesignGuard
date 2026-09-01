using CommunityToolkit.Mvvm.ComponentModel;

namespace DesignGuard.ViewModels;

public sealed partial class PlaybookItemRowViewModel : ObservableObject
{
    private readonly Action<PlaybookItemRowViewModel> _onChanged;

    public PlaybookItemRowViewModel(string id, string text, bool isCompleted, Action<PlaybookItemRowViewModel> onChanged)
    {
        Id = id;
        Text = text;
        _isCompleted = isCompleted;
        _onChanged = onChanged;
    }

    public string Id { get; }
    public string Text { get; }

    [ObservableProperty] private bool _isCompleted;

    partial void OnIsCompletedChanged(bool value) => _onChanged(this);
}

public sealed class PlaybookPhaseRowViewModel
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Goal { get; init; }
    public required DesignGuard.MainNavSection NavSection { get; init; }
    public required string ProgressText { get; init; }
    public required bool IsCurrent { get; init; }
    public required IReadOnlyList<string> Practices { get; init; }
    public required IReadOnlyList<PlaybookItemRowViewModel> Items { get; init; }
}

public sealed class RiskMatrixCellViewModel
{
    public required int Likelihood { get; init; }
    public required int Impact { get; init; }
    public required int OpenCount { get; init; }
    public string Display => OpenCount == 0 ? "" : OpenCount.ToString();
    public string ToolTip => $"Kans {Likelihood} × impact {Impact}: {OpenCount} open";
}

using CommunityToolkit.Mvvm.ComponentModel;
using DesignGuard;

namespace DesignGuard.ViewModels;

public sealed class WorkspaceNavGroup
{
    public required string Title { get; init; }
    public required IReadOnlyList<WorkspaceNavItem> Items { get; init; }
}

public sealed partial class WorkspaceNavItem : ObservableObject
{
    public required string Title { get; init; }
    public required DesignGuard.MainNavSection Section { get; init; }

    [ObservableProperty] private bool _isSelected;
}

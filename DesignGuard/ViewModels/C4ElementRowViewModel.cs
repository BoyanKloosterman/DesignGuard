using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class C4ElementRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelLabel))]
    private C4Level _level = C4Level.Container;

    public string LevelLabel => C4LevelFormatting.ShortLabel(Level);

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _technology = "";

    [ObservableProperty] private int? _parentId;

    /// <summary>Ouders die bij dit niveau passen (C2→C1, C3→C2, C4→C3).</summary>
    public ObservableCollection<C4ParentPickOption> ParentPickOptions { get; } = new();

    [ObservableProperty] private C4ParentPickOption? _selectedParentPick;

    private bool _suppressParentPick;

    partial void OnSelectedParentPickChanged(C4ParentPickOption? value)
    {
        if (_suppressParentPick) return;
        ParentId = value?.Id;
    }

    partial void OnParentIdChanged(int? value)
    {
        if (_suppressParentPick) return;
        SyncSelectedPickFromParentId();
    }

    /// <summary>Vul lijst opnieuw; aanroepen vanuit MainViewModel na wijzigingen in C4Elements.</summary>
    public void RebuildParentPickOptions(IReadOnlyList<C4ElementRowViewModel> allRows)
    {
        ParentPickOptions.Clear();
        ParentPickOptions.Add(C4ParentPickOption.None);

        var parentLevel = Level switch
        {
            C4Level.Context => (C4Level?)null,
            C4Level.Container => C4Level.Context,
            C4Level.Component => C4Level.Container,
            C4Level.Code => C4Level.Component,
            _ => null
        };

        if (parentLevel == null)
        {
            if (Level == C4Level.Context && ParentId != null)
                ParentId = null;
            SyncSelectedPickFromParentId();
            return;
        }

        foreach (var o in allRows
                     .Where(r => r.Level == parentLevel && r.Id != Id)
                     .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            var nm = string.IsNullOrWhiteSpace(o.Name) ? $"#{o.Id}" : $"{o.Name.Trim()} (#{o.Id})";
            ParentPickOptions.Add(new C4ParentPickOption(o.Id, nm));
        }

        SyncSelectedPickFromParentId();
    }

    public void SyncSelectedPickFromParentId()
    {
        _suppressParentPick = true;
        try
        {
            if (ParentId == null)
            {
                SelectedParentPick = ParentPickOptions.FirstOrDefault(p => p.Id == null) ?? C4ParentPickOption.None;
                return;
            }

            var match = ParentPickOptions.FirstOrDefault(p => p.Id == ParentId);
            if (match != null)
                SelectedParentPick = match;
            else
                SelectedParentPick = new C4ParentPickOption(ParentId, $"#{ParentId} (niet beschikbaar)");
        }
        finally
        {
            _suppressParentPick = false;
        }
    }

    /// <summary>Ouderlabel voor visuele kaarten (na commit / sync).</summary>
    [ObservableProperty] private string _parentDisplayHint = "";

    /// <summary>Selectie-sync tussen grid en C4-kaarten.</summary>
    [ObservableProperty] private bool _isC4VisualSelected;

    /// <summary>Aantal open dreigingen waarvan AffectedComponents deze naam raakt.</summary>
    [ObservableProperty] private int _linkedOpenThreatCount;
}

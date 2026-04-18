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

    /// <summary>Ouderlabel voor visuele kaarten (na commit / sync).</summary>
    [ObservableProperty] private string _parentDisplayHint = "";

    /// <summary>Selectie-sync tussen grid en C4-kaarten.</summary>
    [ObservableProperty] private bool _isC4VisualSelected;

    /// <summary>Aantal open dreigingen waarvan AffectedComponents deze naam raakt.</summary>
    [ObservableProperty] private int _linkedOpenThreatCount;
}

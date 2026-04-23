using CommunityToolkit.Mvvm.ComponentModel;

namespace DesignGuard.ViewModels;

/// <summary>Keuze voor Van/Naar in C4-relaties (element-id of 0 = systeem in scope).</summary>
public sealed class C4RelationEndpointOption(int elementId, string label)
{
    public int ElementId { get; } = elementId;
    public string Label { get; } = label;
}

public partial class C4RelationRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private int _fromElementId;

    [ObservableProperty] private int _toElementId;

    [ObservableProperty] private string _label = "";

    [ObservableProperty] private C4RelationEndpointOption? _selectedFromEndpoint;

    [ObservableProperty] private C4RelationEndpointOption? _selectedToEndpoint;

    private bool _suppress;

    partial void OnSelectedFromEndpointChanged(C4RelationEndpointOption? value)
    {
        if (_suppress || value == null) return;
        FromElementId = value.ElementId;
    }

    partial void OnSelectedToEndpointChanged(C4RelationEndpointOption? value)
    {
        if (_suppress || value == null) return;
        ToElementId = value.ElementId;
    }

    /// <summary>Koppel combobox aan ids na verversen van de endpoint-lijst.</summary>
    public void SyncEndpointSelections(IReadOnlyList<C4RelationEndpointOption> choices)
    {
        _suppress = true;
        try
        {
            SelectedFromEndpoint = choices.FirstOrDefault(c => c.ElementId == FromElementId);
            SelectedToEndpoint = choices.FirstOrDefault(c => c.ElementId == ToElementId);
        }
        finally
        {
            _suppress = false;
        }
    }
}

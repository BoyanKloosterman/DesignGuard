// C4 threatmodel-tab (elementen + koppeling naar open dreigingen via componentnamen).
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Export;
using DesignGuard.Models;
using DesignGuard.Services;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    partial void OnSelectedC4ElementChanged(C4ElementRowViewModel? value)
    {
        foreach (var r in C4Elements)
            r.IsC4VisualSelected = r == value;
    }

    public IReadOnlyList<C4Level> C4LevelChoices { get; } =
        new[] { C4Level.Context, C4Level.Container, C4Level.Component, C4Level.Code };

    /// <summary>Na bewerken in het grid: dreigingstelling + visuele banden bijwerken.</summary>
    public void RefreshC4AfterGridEdit() => RefreshC4ThreatLinkCounts();

    partial void OnC4MermaidBandChanged(C4MermaidBand value) => RunRefreshC4MermaidDiagram();

    /// <summary>Zet C4MermaidCode vanuit de tabel (zelfde model als opslaan).</summary>
    [RelayCommand]
    private void RefreshC4MermaidDiagram() => RunRefreshC4MermaidDiagram();

    private void RunRefreshC4MermaidDiagram()
    {
        try
        {
            var m = BuildModelFromEditor();
            C4MermaidCode = _c4MermaidBuilder.Build(C4MermaidBand, m);
            C4MermaidSyntaxError = string.Empty;
        }
        catch (Exception ex)
        {
            C4MermaidSyntaxError = "C4-Mermaid: " + ex.Message;
        }
    }

    partial void OnC4MermaidSyntaxErrorChanged(string value)
    {
        HasC4MermaidError = !string.IsNullOrWhiteSpace(value);
    }

    private void SyncC4VisualBandCollections()
    {
        static void refill(ObservableCollection<C4ElementRowViewModel> band, IEnumerable<C4ElementRowViewModel> src,
            C4Level level)
        {
            band.Clear();
            foreach (var x in src.Where(e => e.Level == level).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
                band.Add(x);
        }

        refill(C4VisualContext, C4Elements, C4Level.Context);
        refill(C4VisualContainers, C4Elements, C4Level.Container);
        refill(C4VisualComponents, C4Elements, C4Level.Component);
        refill(C4VisualCode, C4Elements, C4Level.Code);

        UpdateC4ParentDisplayHints();
    }

    private void UpdateC4ParentDisplayHints()
    {
        var byId = C4Elements.ToDictionary(x => x.Id, x =>
            string.IsNullOrWhiteSpace(x.Name) ? $"#{x.Id}" : x.Name.Trim());

        foreach (var r in C4Elements)
        {
            if (r.ParentId is { } pid && byId.TryGetValue(pid, out var nm))
                r.ParentDisplayHint = $"Ouder: {nm} (#{pid})";
            else if (r.ParentId is { } p2)
                r.ParentDisplayHint = $"Ouder: (onbekend #{p2})";
            else
                r.ParentDisplayHint = "";
        }
    }

    private void RefreshC4ThreatLinkCounts()
    {
        RefreshC4ParentPickLists();
        RefreshC4RelationEndpointChoices();
        foreach (var row in C4Elements)
            row.LinkedOpenThreatCount = C4ExportPresentation.CountOpenThreatMatchesForComponentName(row.Name, Threats);

        SyncC4VisualBandCollections();
        RunRefreshC4MermaidDiagram();
    }

    /// <summary>Endpoint-keuzes voor C4-relaties; na wijziging C4-elementen.</summary>
    private void RefreshC4RelationEndpointChoices()
    {
        C4RelationEndpointChoices.Clear();
        C4RelationEndpointChoices.Add(new C4RelationEndpointOption(0, "Systeem in scope (C1)"));
        foreach (var el in C4Elements.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var nm = string.IsNullOrWhiteSpace(el.Name) ? $"#{el.Id}" : el.Name.Trim();
            C4RelationEndpointChoices.Add(new C4RelationEndpointOption(el.Id, $"{nm} (#{el.Id}) — {el.LevelLabel}"));
        }

        var snap = C4RelationEndpointChoices.ToList();
        foreach (var r in C4Relations)
            r.SyncEndpointSelections(snap);
    }

    /// <summary>Ouder-comboboxen: geldige ouders per C4-niveau.</summary>
    private void RefreshC4ParentPickLists()
    {
        var list = C4Elements.ToList();
        foreach (var row in list)
            row.RebuildParentPickOptions(list);
    }

    [RelayCommand]
    private void SelectC4Element(C4ElementRowViewModel? row)
    {
        if (row != null)
            SelectedC4Element = row;
    }

    [RelayCommand]
    private void AddC4Element()
    {
        var nextId = C4Elements.Count == 0 ? 1 : C4Elements.Max(x => x.Id) + 1;
        C4Elements.Add(new C4ElementRowViewModel
        {
            Id = nextId,
            Level = C4Level.Container,
            Name = "",
            Description = "",
            Technology = ""
        });
        RefreshC4ThreatLinkCounts();
    }

    [RelayCommand]
    private void RemoveC4Element(C4ElementRowViewModel? row)
    {
        if (row == null) return;
        foreach (var rel in C4Relations.Where(r => r.FromElementId == row.Id || r.ToElementId == row.Id).ToList())
        {
            C4Relations.Remove(rel);
            if (SelectedC4Relation == rel)
                SelectedC4Relation = null;
        }

        C4Elements.Remove(row);
        if (SelectedC4Element == row)
            SelectedC4Element = null;
        RefreshC4ThreatLinkCounts();
    }

    [RelayCommand]
    private void AddC4Relation()
    {
        RefreshC4RelationEndpointChoices();
        var nextId = C4Relations.Count == 0 ? 1 : C4Relations.Max(x => x.Id) + 1;
        var toId = C4Elements.FirstOrDefault(e => e.Id > 0)?.Id ?? 0;
        var row = new C4RelationRowViewModel
        {
            Id = nextId,
            FromElementId = 0,
            ToElementId = toId,
            Label = ""
        };
        row.SyncEndpointSelections(C4RelationEndpointChoices.ToList());
        C4Relations.Add(row);
        SelectedC4Relation = row;
        RefreshC4AfterGridEdit();
    }

    [RelayCommand]
    private void RemoveC4Relation(C4RelationRowViewModel? row)
    {
        if (row == null) return;
        C4Relations.Remove(row);
        if (SelectedC4Relation == row)
            SelectedC4Relation = null;
        RefreshC4AfterGridEdit();
    }
}

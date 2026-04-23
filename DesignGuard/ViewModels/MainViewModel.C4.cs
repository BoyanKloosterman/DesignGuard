// C4 threatmodel-tab (elementen + koppeling naar open dreigingen via componentnamen).
using System.Collections.ObjectModel;
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
        foreach (var row in C4Elements)
            row.LinkedOpenThreatCount = C4ExportPresentation.CountOpenThreatMatchesForComponentName(row.Name, Threats);

        SyncC4VisualBandCollections();
        RunRefreshC4MermaidDiagram();
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
        RefreshC4ThreatLinkCounts(); // telt + sync visuele banden
    }

    [RelayCommand]
    private void RemoveC4Element(C4ElementRowViewModel? row)
    {
        if (row == null) return;
        C4Elements.Remove(row);
        if (SelectedC4Element == row)
            SelectedC4Element = null;
        RefreshC4ThreatLinkCounts();
    }
}

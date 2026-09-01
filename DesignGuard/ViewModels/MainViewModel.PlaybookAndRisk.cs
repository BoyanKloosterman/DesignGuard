using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard;
using DesignGuard.Models;
using DesignGuard.Services;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private string _playbookDisclaimer = "";

    [ObservableProperty] private string _playbookCurrentPhaseTitle = "";

    [ObservableProperty] private string _playbookCurrentPhaseGoal = "";

    [ObservableProperty] private string _playbookNextAction = "";

    [ObservableProperty] private MainNavSection _playbookCurrentNav = MainNavSection.Dashboard;

    [ObservableProperty] private ObservableCollection<PlaybookPhaseRowViewModel> _playbookPhases = new();

    [ObservableProperty] private ObservableCollection<PlaybookItemRowViewModel> _playbookCurrentItems = new();

    [ObservableProperty] private ObservableCollection<string> _playbookCurrentPractices = new();

    [ObservableProperty] private ObservableCollection<RiskMatrixCellViewModel> _riskMatrixCells = new();

    [ObservableProperty] private ObservableCollection<ThreatModel> _riskRegister = new();

    [ObservableProperty] private string _riskSummaryText = "";

    [RelayCommand]
    private void GoToPlaybookPhase() => NavSection = PlaybookCurrentNav;

    private void RefreshPlaybook()
    {
        var book = _playbook.Load();
        PlaybookDisclaimer = book.Disclaimer;
        PlaybookPhases.Clear();
        PlaybookCurrentItems.Clear();
        PlaybookCurrentPractices.Clear();

        PentestPlaybookPhase? current = null;
        foreach (var phase in book.Phases)
        {
            var done = phase.Items.Count(i => _completedPlaybookItemIds.Contains(i.Id));
            var isCurrent = current == null && done < phase.Items.Count;
            if (isCurrent) current = phase;
            var items = phase.Items.Select(i => new PlaybookItemRowViewModel(
                i.Id,
                i.Text,
                _completedPlaybookItemIds.Contains(i.Id),
                OnPlaybookItemChanged)).ToList();
            PlaybookPhases.Add(new PlaybookPhaseRowViewModel
            {
                Id = phase.Id,
                Title = phase.Title,
                Goal = phase.Goal,
                NavSection = phase.NavSection,
                ProgressText = $"{done}/{phase.Items.Count}",
                IsCurrent = isCurrent,
                Practices = phase.Practices,
                Items = items
            });
        }

        current ??= book.Phases.LastOrDefault();
        if (current == null)
        {
            PlaybookCurrentPhaseTitle = "";
            PlaybookCurrentPhaseGoal = "";
            PlaybookNextAction = HasOpenProject
                ? "Playbook ontbreekt."
                : "Open of maak een project om de aanpak te starten.";
            return;
        }

        PlaybookCurrentPhaseTitle = current.Title;
        PlaybookCurrentPhaseGoal = current.Goal;
        PlaybookCurrentNav = current.NavSection;
        var open = current.Items.FirstOrDefault(i => !_completedPlaybookItemIds.Contains(i.Id));
        PlaybookNextAction = open == null
            ? "Aanpak afgerond. Exporteer het rapport."
            : $"Volgende: {open.Text}";

        var row = PlaybookPhases.FirstOrDefault(p => p.Id == current.Id);
        if (row == null) return;
        foreach (var p in row.Practices)
            PlaybookCurrentPractices.Add(p);
        foreach (var i in row.Items)
            PlaybookCurrentItems.Add(i);
    }

    private void OnPlaybookItemChanged(PlaybookItemRowViewModel row)
    {
        if (row.IsCompleted) _completedPlaybookItemIds.Add(row.Id);
        else _completedPlaybookItemIds.Remove(row.Id);
        RefreshPlaybook();
    }

    private void RefreshRiskAnalysis()
    {
        RiskMatrixCells.Clear();
        for (var impact = 5; impact >= 1; impact--)
        {
            for (var likelihood = 1; likelihood <= 5; likelihood++)
            {
                var i = impact;
                var l = likelihood;
                var open = Threats.Count(t =>
                    t.Status == ThreatStatus.Open && t.Likelihood == l && t.Impact == i);
                RiskMatrixCells.Add(new RiskMatrixCellViewModel
                {
                    Likelihood = l,
                    Impact = i,
                    OpenCount = open
                });
            }
        }

        var ordered = Threats
            .OrderByDescending(t => t.RiskScore)
            .ThenBy(t => t.Title)
            .ToList();
        RiskRegister = new ObservableCollection<ThreatModel>(ordered);

        var openList = Threats.Where(t => t.Status == ThreatStatus.Open).ToList();
        var highOpen = openList.Count(t => t.RiskLevel is RiskLevel.High or RiskLevel.Critical);
        var accepted = Threats.Count(t => t.Status == ThreatStatus.Accepted);
        var mitigated = Threats.Count(t => t.Status == ThreatStatus.Mitigated);
        var maxOpen = openList.Count == 0 ? 0 : openList.Max(t => t.RiskScore);
        RiskSummaryText =
            $"Open: {openList.Count} (hoog/kritiek: {highOpen}, hoogste score: {maxOpen}). " +
            $"Gemitigeerd: {mitigated}. Geaccepteerd: {accepted}. Alleen open dreigingen tellen in de heatmap.";
    }

    public void OnThreatRiskChanged(ThreatModel t)
    {
        RiskScoring.SyncSeverity(t);
        t.UserModified = true;
        RefreshFilters();
        RefreshRiskAnalysis();
        UpdateDashboard();
        OnPropertyChanged(nameof(SelectedThreat));
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard;
using DesignGuard.Models;
using DesignGuard.Services;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    private string? _selectedPlaybookPhaseId;

    [ObservableProperty] private string _playbookDisclaimer = "";

    [ObservableProperty] private string _playbookCurrentPhaseTitle = "";

    [ObservableProperty] private string _playbookCurrentPhaseGoal = "";

    [ObservableProperty] private string _playbookNextAction = "";

    [ObservableProperty] private MainNavSection _playbookCurrentNav = MainNavSection.Dashboard;

    [ObservableProperty] private string _selectedPlaybookPhaseTitle = "";

    [ObservableProperty] private string _selectedPlaybookPhaseGoal = "";

    [ObservableProperty] private MainNavSection _selectedPlaybookNav = MainNavSection.Pentest;

    [ObservableProperty] private ObservableCollection<PlaybookPhaseRowViewModel> _playbookPhases = new();

    [ObservableProperty] private ObservableCollection<PlaybookItemRowViewModel> _playbookCurrentItems = new();

    [ObservableProperty] private ObservableCollection<string> _playbookCurrentPractices = new();

    [ObservableProperty] private ObservableCollection<PlaybookItemRowViewModel> _selectedPlaybookItems = new();

    [ObservableProperty] private ObservableCollection<string> _selectedPlaybookPractices = new();

    [ObservableProperty] private ObservableCollection<RiskMatrixCellViewModel> _riskMatrixCells = new();

    [ObservableProperty] private ObservableCollection<RiskMatrixCellViewModel> _findingRiskMatrixCells = new();

    [ObservableProperty] private ObservableCollection<ThreatModel> _riskRegister = new();

    [ObservableProperty] private ObservableCollection<PentestFindingModel> _findingRiskRegister = new();

    [ObservableProperty] private string _riskSummaryText = "";

    [RelayCommand]
    private void GoToPlaybookPhase() => NavSection = PlaybookCurrentNav;

    [RelayCommand]
    private void GoToSelectedPlaybookPhase() => NavSection = SelectedPlaybookNav;

    [RelayCommand]
    private void SelectPlaybookPhase(PlaybookPhaseRowViewModel? row)
    {
        if (row == null) return;
        _selectedPlaybookPhaseId = row.Id;
        RefreshPlaybook();
    }

    private void RefreshPlaybook()
    {
        var book = _playbook.Load();
        PlaybookDisclaimer = book.Disclaimer;
        PlaybookPhases.Clear();
        PlaybookCurrentItems.Clear();
        PlaybookCurrentPractices.Clear();
        SelectedPlaybookItems.Clear();
        SelectedPlaybookPractices.Clear();

        string? firstOpenId = null;
        foreach (var phase in book.Phases)
        {
            var done = phase.Items.Count(i => _completedPlaybookItemIds.Contains(i.Id));
            if (firstOpenId == null && done < phase.Items.Count)
                firstOpenId = phase.Id;
        }

        if (_selectedPlaybookPhaseId == null || book.Phases.All(p => p.Id != _selectedPlaybookPhaseId))
            _selectedPlaybookPhaseId = firstOpenId ?? book.Phases.LastOrDefault()?.Id;

        PentestPlaybookPhase? coach = null;
        PentestPlaybookPhase? selected = null;
        foreach (var phase in book.Phases)
        {
            var done = phase.Items.Count(i => _completedPlaybookItemIds.Contains(i.Id));
            var isCurrent = phase.Id == firstOpenId;
            var isSelected = phase.Id == _selectedPlaybookPhaseId;
            if (isCurrent) coach = phase;
            if (isSelected) selected = phase;
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
                IsSelected = isSelected,
                Practices = phase.Practices,
                Items = items
            });
        }

        coach ??= book.Phases.LastOrDefault();
        if (coach == null)
        {
            PlaybookCurrentPhaseTitle = "";
            PlaybookCurrentPhaseGoal = "";
            PlaybookNextAction = HasOpenProject
                ? "Playbook ontbreekt."
                : "Open of maak een project om de aanpak te starten.";
            SelectedPlaybookPhaseTitle = "";
            SelectedPlaybookPhaseGoal = "";
            return;
        }

        PlaybookCurrentPhaseTitle = coach.Title;
        PlaybookCurrentPhaseGoal = coach.Goal;
        PlaybookCurrentNav = coach.NavSection;
        var open = coach.Items.FirstOrDefault(i => !_completedPlaybookItemIds.Contains(i.Id));
        PlaybookNextAction = open == null
            ? "Aanpak afgerond. Exporteer het rapport."
            : $"Volgende: {open.Text}";

        var coachRow = PlaybookPhases.FirstOrDefault(p => p.Id == coach.Id);
        if (coachRow != null)
        {
            foreach (var p in coachRow.Practices)
                PlaybookCurrentPractices.Add(p);
            foreach (var i in coachRow.Items)
                PlaybookCurrentItems.Add(i);
        }

        selected ??= coach;
        SelectedPlaybookPhaseTitle = selected.Title;
        SelectedPlaybookPhaseGoal = selected.Goal;
        SelectedPlaybookNav = selected.NavSection;
        var selectedRow = PlaybookPhases.FirstOrDefault(p => p.Id == selected.Id);
        if (selectedRow == null) return;
        foreach (var p in selectedRow.Practices)
            SelectedPlaybookPractices.Add(p);
        foreach (var i in selectedRow.Items)
            SelectedPlaybookItems.Add(i);
    }

    private void OnPlaybookItemChanged(PlaybookItemRowViewModel row)
    {
        if (row.IsCompleted) _completedPlaybookItemIds.Add(row.Id);
        else _completedPlaybookItemIds.Remove(row.Id);
        RefreshPlaybook();
    }

    private void RefreshRiskAnalysis()
    {
        FillRiskMatrix(RiskMatrixCells, (l, i) =>
            Threats.Count(t => t.Status == ThreatStatus.Open && t.Likelihood == l && t.Impact == i));
        FillRiskMatrix(FindingRiskMatrixCells, (l, i) =>
            Findings.Count(f => f.CountsInHeatmap && f.Likelihood == l && f.Impact == i));

        RiskRegister = new ObservableCollection<ThreatModel>(
            Threats.OrderByDescending(t => t.RiskScore).ThenBy(t => t.Title));
        FindingRiskRegister = new ObservableCollection<PentestFindingModel>(
            Findings.OrderByDescending(f => f.RiskScore).ThenBy(f => f.Title));

        var openList = Threats.Where(t => t.Status == ThreatStatus.Open).ToList();
        var highOpen = openList.Count(t => t.RiskLevel is RiskLevel.High or RiskLevel.Critical);
        var accepted = Threats.Count(t => t.Status == ThreatStatus.Accepted);
        var mitigated = Threats.Count(t => t.Status == ThreatStatus.Mitigated);
        var maxOpen = openList.Count == 0 ? 0 : openList.Max(t => t.RiskScore);

        var openFindings = Findings.Where(f => f.CountsInHeatmap).ToList();
        var highFindings = openFindings.Count(f => f.RiskLevel is RiskLevel.High or RiskLevel.Critical);
        var maxFind = openFindings.Count == 0 ? 0 : openFindings.Max(f => f.RiskScore);
        OpenFindingCount = openFindings.Count;
        RiskSummaryText =
            $"Dreigingen open: {openList.Count} (hoog/kritiek: {highOpen}, hoogste score: {maxOpen}; " +
            $"gemitigeerd: {mitigated}, geaccepteerd: {accepted}). " +
            $"Bevindingen open: {openFindings.Count} (hoog/kritiek: {highFindings}, hoogste score: {maxFind}).";
    }

    private static void FillRiskMatrix(
        ObservableCollection<RiskMatrixCellViewModel> cells,
        Func<int, int, int> openCount)
    {
        cells.Clear();
        for (var impact = 5; impact >= 1; impact--)
        {
            for (var likelihood = 1; likelihood <= 5; likelihood++)
            {
                var i = impact;
                var l = likelihood;
                cells.Add(new RiskMatrixCellViewModel
                {
                    Likelihood = l,
                    Impact = i,
                    OpenCount = openCount(l, i)
                });
            }
        }
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

    public void OnFindingRiskChanged(PentestFindingModel _)
    {
        RefreshFilters();
        RefreshRiskAnalysis();
        UpdateDashboard();
        OnPropertyChanged(nameof(SelectedFinding));
    }

    [RelayCommand]
    private void AddFinding()
    {
        var f = new PentestFindingModel
        {
            Title = "Nieuwe bevinding",
            Description = "Beschrijf de observatie, zonder exploit-stappen.",
            WstgCategory = "Overig",
            Likelihood = 3,
            Impact = 3,
            Status = FindingStatus.Open
        };
        Findings.Add(f);
        SelectedFinding = f;
        RefreshFilters();
        RefreshRiskAnalysis();
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveFinding(PentestFindingModel? f)
    {
        if (f == null) return;
        Findings.Remove(f);
        if (SelectedFinding == f) SelectedFinding = null;
        RefreshFilters();
        RefreshRiskAnalysis();
        UpdateDashboard();
    }

    [RelayCommand]
    private void AddAttackSurface()
    {
        AttackSurface.Add(new AttackSurfaceItemModel { Kind = "URL", Value = "https://" });
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveAttackSurface(AttackSurfaceItemModel? item)
    {
        if (item == null) return;
        AttackSurface.Remove(item);
        UpdateDashboard();
    }

    [RelayCommand]
    private void AddTestBlocker()
    {
        TestBlockers.Add(new TestBlockerModel { Title = "Nieuwe blokkade" });
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveTestBlocker(TestBlockerModel? item)
    {
        if (item == null) return;
        TestBlockers.Remove(item);
        UpdateDashboard();
    }
}

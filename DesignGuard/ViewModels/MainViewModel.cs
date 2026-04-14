using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Export;
using DesignGuard.Models;
using DesignGuard.Services;
using Microsoft.Win32;

namespace DesignGuard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IProjectRepository _projects;
    private readonly ThreatGenerationService _threatService;
    private readonly RequirementGenerationService _requirementService;
    private readonly DiagramLayoutService _diagramLayout;
    private readonly ExportService _export;
    private readonly AnalysisMergeService _merge;
    private readonly TraceabilityService _traceability;
    private readonly ProjectTemplateService _templates;

    public MainViewModel(
        IProjectRepository projects,
        ThreatGenerationService threatService,
        RequirementGenerationService requirementService,
        DiagramLayoutService diagramLayout,
        ExportService export,
        AnalysisMergeService merge,
        TraceabilityService traceability,
        ProjectTemplateService templates)
    {
        _projects = projects;
        _threatService = threatService;
        _requirementService = requirementService;
        _diagramLayout = diagramLayout;
        _export = export;
        _merge = merge;
        _traceability = traceability;
        _templates = templates;
        SystemTypeOptions = Enum.GetNames(typeof(SystemType)).ToList();
        DeploymentContextOptions = Enum.GetNames(typeof(DeploymentContext)).ToList();
        ThreatStatusOptions = Enum.GetNames(typeof(ThreatStatus)).ToList();
        SeverityOptions = Enum.GetNames(typeof(SeverityEstimate)).ToList();
        RequirementStatusOptions = Enum.GetNames(typeof(RequirementStatus)).ToList();
        PriorityOptions = Enum.GetNames(typeof(RequirementPriority)).ToList();
        DesignNoteKindOptions = Enum.GetNames(typeof(DesignNoteKind)).ToList();
        FilteredThreats = new ObservableCollection<ThreatModel>();
        FilteredRequirements = new ObservableCollection<RequirementModel>();
    }

    public IReadOnlyList<string> SystemTypeOptions { get; }
    public IReadOnlyList<string> DeploymentContextOptions { get; }
    public IReadOnlyList<string> ThreatStatusOptions { get; }
    public IReadOnlyList<string> SeverityOptions { get; }
    public IReadOnlyList<string> RequirementStatusOptions { get; }
    public IReadOnlyList<string> PriorityOptions { get; }
    public IReadOnlyList<string> DesignNoteKindOptions { get; }
    public IReadOnlyList<(string Key, string Title, string Description)> TemplateList => _templates.ListTemplates();

    public IReadOnlyList<ThreatStatus> AllThreatStatuses { get; } =
        Enum.GetValues(typeof(ThreatStatus)).Cast<ThreatStatus>().ToArray();

    public IReadOnlyList<StrideCategory> AllStrideCategories { get; } =
        Enum.GetValues(typeof(StrideCategory)).Cast<StrideCategory>().ToArray();

    public IReadOnlyList<SeverityEstimate> AllSeverities { get; } =
        Enum.GetValues(typeof(SeverityEstimate)).Cast<SeverityEstimate>().ToArray();

    public IReadOnlyList<RequirementStatus> AllRequirementStatuses { get; } =
        Enum.GetValues(typeof(RequirementStatus)).Cast<RequirementStatus>().ToArray();

    public IReadOnlyList<RequirementPriority> AllRequirementPriorities { get; } =
        Enum.GetValues(typeof(RequirementPriority)).Cast<RequirementPriority>().ToArray();

    [ObservableProperty] private ObservableCollection<ProjectSummaryItem> _projectList = new();

    [ObservableProperty] private ProjectSummaryItem? _selectedProjectSummary;

    /// <summary>0 Dashboard, 1 Ontwerp, 2 Dreigingen, 3 Eisen, 4 Beslissingen, 5 Traceability, 6 Export</summary>
    [ObservableProperty] private int _navSection;

    [ObservableProperty] private string _statusMessage = "DesignGuard v2 — lokaal security-by-design.";

    [ObservableProperty] private int _currentProjectId;

    [ObservableProperty] private string _editorProjectName = "";

    [ObservableProperty] private string _editorProjectDescription = "";

    [ObservableProperty] private string _editorSystemName = "";

    [ObservableProperty] private string _editorSystemType = SystemType.WebApp.ToString();

    [ObservableProperty] private string _editorDeploymentContext = DeploymentContext.Cloud.ToString();

    [ObservableProperty] private bool _flagInternetExposed = true;

    [ObservableProperty] private bool _flagPersonalData;

    [ObservableProperty] private bool _flagAuth;

    [ObservableProperty] private bool _flagAdmin;

    [ObservableProperty] private bool _flagExternalApi;

    [ObservableProperty] private bool _flagUpload;

    [ObservableProperty] private bool _flagSensitiveStorage;

    [ObservableProperty] private bool _flagLoggingMonitoring = true;

    [ObservableProperty] private bool _flagCriticalBusiness;

    [ObservableProperty] private string _openIssuesSummary = "";

    [ObservableProperty] private ObservableCollection<TrustBoundaryRowViewModel> _trustBoundaries = new();

    [ObservableProperty] private ObservableCollection<ComponentRowViewModel> _components = new();

    [ObservableProperty] private ObservableCollection<DataFlowRowViewModel> _dataFlows = new();

    [ObservableProperty] private ObservableCollection<RoleRowViewModel> _roles = new();

    [ObservableProperty] private ObservableCollection<AssetRowViewModel> _assets = new();

    [ObservableProperty] private ObservableCollection<DesignNoteRowViewModel> _designNotes = new();

    [ObservableProperty] private ObservableCollection<ControlRowViewModel> _controls = new();

    [ObservableProperty] private ObservableCollection<DiagramNodeViewModel> _diagramNodes = new();

    [ObservableProperty] private ObservableCollection<DiagramLineViewModel> _diagramLines = new();

    [ObservableProperty] private ObservableCollection<TrustBoundaryOverlayViewModel> _diagramTrustOverlays = new();

    [ObservableProperty] private ObservableCollection<ThreatModel> _threats = new();

    [ObservableProperty] private ObservableCollection<RequirementModel> _requirements = new();

    [ObservableProperty] private ObservableCollection<ThreatModel> _filteredThreats = new();

    [ObservableProperty] private ObservableCollection<RequirementModel> _filteredRequirements = new();

    [ObservableProperty] private string _threatFilterText = "";

    [ObservableProperty] private string _requirementFilterText = "";

    [ObservableProperty] private string _threatSort = "Severity";

    [ObservableProperty] private string _requirementSort = "Priority";

    [ObservableProperty] private string _traceabilityText = "";

    [ObservableProperty] private int _openThreatCount;

    [ObservableProperty] private int _mitigatedThreatCount;

    [ObservableProperty] private int _openRequirementCount;

    [ObservableProperty] private int _implementedRequirementCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProjectOverviewInDetails))]
    private ThreatModel? _selectedThreat;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProjectOverviewInDetails))]
    private RequirementModel? _selectedRequirement;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProjectOverviewInDetails))]
    private ComponentRowViewModel? _selectedComponent;

    [ObservableProperty] private string _exportPreview = "";

    public bool ShowProjectOverviewInDetails =>
        SelectedThreat == null && SelectedRequirement == null && SelectedComponent == null;

    partial void OnSelectedProjectSummaryChanged(ProjectSummaryItem? value)
    {
        if (value == null)
        {
            ClearEditor();
            return;
        }

        _ = LoadProjectAsync(value.Id);
    }

    partial void OnNavSectionChanged(int value)
    {
        if (value == 1) RefreshDiagram();
        if (value is 2 or 3)
        {
            RefreshFilters();
            UpdateDashboard();
        }

        if (value == 5) RefreshTraceability();
        if (value == 6) RefreshExportPreview();
    }

    partial void OnThreatFilterTextChanged(string value) => RefreshFilters();

    partial void OnRequirementFilterTextChanged(string value) => RefreshFilters();

    partial void OnThreatSortChanged(string value) => RefreshFilters();

    partial void OnRequirementSortChanged(string value) => RefreshFilters();

    [RelayCommand]
    private async Task InitializeAsync()
    {
        try
        {
            await _projects.EnsureDatabaseAsync();
            await _projects.EnsureDemoProjectAsync();
            await ReloadProjectListAsync();
            var demo = ProjectList.FirstOrDefault(p => p.Name.StartsWith("Demo", StringComparison.Ordinal));
            SelectedProjectSummary = demo ?? ProjectList.FirstOrDefault();
            StatusMessage = "Database gereed (v2). Demo-project beschikbaar.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Initialisatie mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ReloadProjectListAsync()
    {
        var list = await _projects.ListSummariesAsync();
        ProjectList = new ObservableCollection<ProjectSummaryItem>(list.Select(p =>
            new ProjectSummaryItem { Id = p.Id, Name = p.Name, UpdatedAtUtc = p.UpdatedAtUtc }));
    }

    private async Task LoadProjectAsync(int id)
    {
        try
        {
            var p = await _projects.GetAsync(id);
            if (p == null)
            {
                StatusMessage = "Project niet gevonden.";
                return;
            }

            ApplyModelToEditor(p);
            if (p.Threats.Count == 0 && p.Requirements.Count == 0)
                RegenerateFromDesign();
            RefreshDiagram();
            RefreshFilters();
            UpdateDashboard();
            RefreshTraceability();
            StatusMessage = $"Project geladen: {p.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Laden mislukt: {ex.Message}";
        }
    }

    private void ClearEditor()
    {
        CurrentProjectId = 0;
        EditorProjectName = "";
        EditorProjectDescription = "";
        EditorSystemName = "";
        EditorSystemType = SystemType.WebApp.ToString();
        EditorDeploymentContext = DeploymentContext.Cloud.ToString();
        FlagInternetExposed = true;
        FlagPersonalData = FlagAuth = FlagAdmin = FlagExternalApi = FlagUpload = FlagSensitiveStorage = false;
        FlagLoggingMonitoring = true;
        FlagCriticalBusiness = false;
        OpenIssuesSummary = "";
        TrustBoundaries.Clear();
        Components.Clear();
        DataFlows.Clear();
        Roles.Clear();
        Assets.Clear();
        DesignNotes.Clear();
        Controls.Clear();
        Threats.Clear();
        Requirements.Clear();
        DiagramNodes.Clear();
        DiagramLines.Clear();
        DiagramTrustOverlays.Clear();
        ExportPreview = "";
        SelectedThreat = null;
        SelectedRequirement = null;
        SelectedComponent = null;
        TraceabilityText = "";
        RefreshFilters();
        UpdateDashboard();
    }

    private void ApplyModelToEditor(ProjectModel p)
    {
        CurrentProjectId = p.Id;
        EditorProjectName = p.Name;
        EditorProjectDescription = p.Description;
        EditorSystemName = p.SystemName;
        EditorSystemType = p.SystemType.ToString();
        EditorDeploymentContext = p.DeploymentContext.ToString();
        FlagInternetExposed = p.InternetExposed;
        FlagPersonalData = p.PersonalDataProcessed;
        FlagAuth = p.HasAuthentication;
        FlagAdmin = p.HasAdmin;
        FlagExternalApi = p.ExternalApis;
        FlagUpload = p.FileUpload;
        FlagSensitiveStorage = p.SensitiveDataStored;
        FlagLoggingMonitoring = p.LoggingMonitoringPresent;
        FlagCriticalBusiness = p.CriticalBusinessFunction;
        OpenIssuesSummary = p.OpenIssuesSummary;

        TrustBoundaries.Clear();
        foreach (var b in p.TrustBoundaries)
        {
            TrustBoundaries.Add(new TrustBoundaryRowViewModel
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description,
                Notes = b.Notes,
                ColorHint = b.ColorHint
            });
        }

        Components.Clear();
        foreach (var c in p.Components)
        {
            Components.Add(new ComponentRowViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag,
                TrustBoundaryId = c.TrustBoundaryId,
                TrustBoundaryName = c.TrustBoundaryName,
                IsEntryPoint = c.IsEntryPoint,
                AssetClassification = c.AssetClassification.ToString(),
                DataSensitivity = c.StoresOrProcesses.ToString(),
                Notes = c.Notes,
                VisualX = c.VisualX,
                VisualY = c.VisualY
            });
        }

        Roles.Clear();
        foreach (var r in p.UserRoles)
        {
            Roles.Add(new RoleRowViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description
            });
        }

        DataFlows.Clear();
        var compById = Components.ToDictionary(c => c.Id);
        foreach (var f in p.DataFlows)
        {
            compById.TryGetValue(f.FromComponentId, out var from);
            compById.TryGetValue(f.ToComponentId, out var to);
            DataFlows.Add(new DataFlowRowViewModel
            {
                From = from,
                To = to,
                Label = f.Label,
                Notes = f.Notes
            });
        }

        Assets.Clear();
        foreach (var a in p.Assets)
        {
            Assets.Add(new AssetRowViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Classification = a.Classification.ToString(),
                Sensitivity = a.Sensitivity.ToString(),
                Notes = a.Notes,
                RelatedComponentId = a.RelatedComponentId
            });
        }

        DesignNotes.Clear();
        foreach (var n in p.DesignNotes)
        {
            DesignNotes.Add(new DesignNoteRowViewModel
            {
                Id = n.Id,
                Kind = n.Kind.ToString(),
                Title = n.Title,
                Description = n.Description,
                Notes = n.Notes
            });
        }

        Controls.Clear();
        foreach (var c in p.Controls)
        {
            Controls.Add(new ControlRowViewModel
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                LinkedThreatStableId = c.LinkedThreatStableId,
                StatusNotes = c.StatusNotes
            });
        }

        Threats = new ObservableCollection<ThreatModel>(p.Threats);
        Requirements = new ObservableCollection<RequirementModel>(p.Requirements);
        RefreshFilters();
        UpdateDashboard();
    }

    private ProjectModel BuildModelFromEditor()
    {
        if (!Enum.TryParse<SystemType>(EditorSystemType, out var st))
            st = SystemType.WebApp;
        if (!Enum.TryParse<DeploymentContext>(EditorDeploymentContext, out var dep))
            dep = DeploymentContext.Cloud;

        var tbList = TrustBoundaries.Select(b => new TrustBoundaryModel
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            Notes = b.Notes,
            ColorHint = b.ColorHint
        }).ToList();

        var compList = Components.Select(c => new ComponentModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Tag = c.Tag,
            TrustBoundaryId = c.TrustBoundaryId,
            TrustBoundaryName = c.TrustBoundaryName,
            IsEntryPoint = c.IsEntryPoint,
            AssetClassification = Enum.TryParse<AssetClassification>(c.AssetClassification, out var ac)
                ? ac
                : AssetClassification.Unspecified,
            StoresOrProcesses = Enum.TryParse<DataSensitivity>(c.DataSensitivity, out var ds)
                ? ds
                : DataSensitivity.None,
            Notes = c.Notes,
            VisualX = c.VisualX,
            VisualY = c.VisualY
        }).ToList();

        var flows = new List<DataFlowModel>();
        foreach (var f in DataFlows)
        {
            if (f.From == null || f.To == null) continue;
            flows.Add(new DataFlowModel
            {
                FromComponentId = f.From.Id,
                ToComponentId = f.To.Id,
                Label = f.Label,
                Notes = f.Notes
            });
        }

        var roleList = Roles.Select(r => new UserRoleModel
        {
            Id = r.Id,
            Name = r.Name,
            Description = r.Description
        }).ToList();

        var assetList = Assets.Select(a => new AssetModel
        {
            Id = a.Id,
            Name = a.Name,
            Description = a.Description,
            Classification = Enum.TryParse<AssetClassification>(a.Classification, out var cl)
                ? cl
                : AssetClassification.Unspecified,
            Sensitivity = Enum.TryParse<DataSensitivity>(a.Sensitivity, out var se)
                ? se
                : DataSensitivity.None,
            Notes = a.Notes,
            RelatedComponentId = a.RelatedComponentId
        }).ToList();

        var notes = DesignNotes.Select(n => new DesignNoteModel
        {
            Id = n.Id,
            Kind = Enum.TryParse<DesignNoteKind>(n.Kind, out var k) ? k : DesignNoteKind.Assumption,
            Title = n.Title,
            Description = n.Description,
            Notes = n.Notes
        }).ToList();

        var ctrl = Controls.Select(c => new ControlModel
        {
            Id = c.Id,
            Title = c.Title,
            Description = c.Description,
            LinkedThreatStableId = c.LinkedThreatStableId,
            StatusNotes = c.StatusNotes
        }).ToList();

        return new ProjectModel
        {
            Id = CurrentProjectId,
            Name = EditorProjectName,
            Description = EditorProjectDescription,
            SystemName = EditorSystemName,
            SystemType = st,
            DeploymentContext = dep,
            InternetExposed = FlagInternetExposed,
            PersonalDataProcessed = FlagPersonalData,
            HasAuthentication = FlagAuth,
            HasAdmin = FlagAdmin,
            ExternalApis = FlagExternalApi,
            FileUpload = FlagUpload,
            SensitiveDataStored = FlagSensitiveStorage,
            LoggingMonitoringPresent = FlagLoggingMonitoring,
            CriticalBusinessFunction = FlagCriticalBusiness,
            OpenIssuesSummary = OpenIssuesSummary,
            TrustBoundaries = tbList,
            Components = compList,
            DataFlows = flows,
            UserRoles = roleList,
            Assets = assetList,
            DesignNotes = notes,
            Controls = ctrl,
            Threats = Threats.ToList(),
            Requirements = Requirements.ToList()
        };
    }

    [RelayCommand]
    private void NewProject()
    {
        SelectedProjectSummary = null;
        ClearEditor();
        EditorProjectName = "Nieuw project";
        EditorSystemName = "Mijn systeem";
        NavSection = 1;
        StatusMessage = "Nieuw project — gebruik de wizard of vul het ontwerp in.";
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        try
        {
            var m = BuildModelFromEditor();
            if (string.IsNullOrWhiteSpace(m.Name))
            {
                StatusMessage = "Projectnaam is verplicht.";
                return;
            }

            var id = await _projects.SaveAsync(m);
            CurrentProjectId = id;
            await ReloadProjectListAsync();
            SelectedProjectSummary = ProjectList.FirstOrDefault(p => p.Id == id);
            ApplyModelToEditor(await _projects.GetAsync(id) ?? m);
            RefreshDiagram();
            StatusMessage = "Opgeslagen.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Opslaan mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteProjectAsync()
    {
        if (CurrentProjectId == 0)
        {
            StatusMessage = "Geen project om te verwijderen.";
            return;
        }

        try
        {
            await _projects.DeleteAsync(CurrentProjectId);
            await ReloadProjectListAsync();
            ClearEditor();
            StatusMessage = "Project verwijderd.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Verwijderen mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EnsureDemoAsync()
    {
        try
        {
            await _projects.EnsureDemoProjectAsync();
            await ReloadProjectListAsync();
            var demo = ProjectList.FirstOrDefault(p => p.Name.StartsWith("Demo", StringComparison.Ordinal));
            if (demo != null)
                SelectedProjectSummary = demo;
            StatusMessage = "Demo-project beschikbaar.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Demo aanmaken mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenProjectWizard()
    {
        var w = new ProjectWizardWindow(this);
        w.Owner = System.Windows.Application.Current.MainWindow;
        w.ShowDialog();
    }

    [RelayCommand]
    private void ApplyTemplate(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        try
        {
            var t = _templates.Create(key);
            SelectedProjectSummary = null;
            ClearEditor();
            ApplyModelToEditor(t);
            CurrentProjectId = 0;
            NavSection = 1;
            StatusMessage = $"Sjabloon geladen: {t.Name}. Sla op om vast te leggen.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sjabloon mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RegenerateFromDesign()
    {
        try
        {
            var m = BuildModelFromEditor();
            var genT = _threatService.Generate(m);
            var genR = _requirementService.Generate(m);
            _merge.MergeThreats(m, genT);
            _merge.MergeRequirements(m, genR);
            RequirementThreatLinker.Link(m);
            Threats = new ObservableCollection<ThreatModel>(m.Threats);
            Requirements = new ObservableCollection<RequirementModel>(m.Requirements);
            RefreshFilters();
            UpdateDashboard();
            RefreshTraceability();
            StatusMessage = "Analyse vernieuwd (samengevoegd met handmatige items).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analyse mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddCustomThreat()
    {
        Threats.Add(new ThreatModel
        {
            Origin = ThreatOrigin.Custom,
            Title = "Handmatige dreiging",
            Description = "Beschrijf het scenario.",
            StrideCategory = StrideCategory.Tampering,
            Severity = SeverityEstimate.Medium,
            Status = ThreatStatus.Open,
            GenerationReason = "Toegevoegd door gebruiker.",
            Explanation = new ExplanationModel
            {
                WhatItMeans = "",
                WhyItMatters = "",
                WhyIncluded = "Handmatig toegevoegd."
            }
        });
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveThreat(ThreatModel? t)
    {
        if (t == null) return;
        Threats.Remove(t);
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void AddCustomRequirement()
    {
        Requirements.Add(new RequirementModel
        {
            Origin = RequirementOrigin.Custom,
            Title = "Handmatige eis",
            Category = "Algemeen",
            PlainExplanation = "",
            WhyApplies = "Toegevoegd door gebruiker.",
            ImplementationDirection = "",
            Priority = RequirementPriority.Medium,
            Status = RequirementStatus.Proposed,
            Explanation = new ExplanationModel { WhatItMeans = "", WhyItMatters = "", WhyIncluded = "" }
        });
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void RemoveRequirement(RequirementModel? r)
    {
        if (r == null) return;
        Requirements.Remove(r);
        RefreshFilters();
        UpdateDashboard();
    }

    [RelayCommand]
    private void AddTrustBoundary()
    {
        TrustBoundaries.Add(new TrustBoundaryRowViewModel { Name = "Nieuwe grens" });
    }

    [RelayCommand]
    private void RemoveTrustBoundary(TrustBoundaryRowViewModel? row)
    {
        if (row == null) return;
        TrustBoundaries.Remove(row);
    }

    [RelayCommand]
    private void AddAsset()
    {
        Assets.Add(new AssetRowViewModel { Name = "Asset" });
    }

    [RelayCommand]
    private void RemoveAsset(AssetRowViewModel? row)
    {
        if (row == null) return;
        Assets.Remove(row);
    }

    [RelayCommand]
    private void AddDesignNote()
    {
        DesignNotes.Add(new DesignNoteRowViewModel
        {
            Kind = DesignNoteKind.Assumption.ToString(),
            Title = "Nieuwe notitie"
        });
    }

    [RelayCommand]
    private void RemoveDesignNote(DesignNoteRowViewModel? row)
    {
        if (row == null) return;
        DesignNotes.Remove(row);
    }

    [RelayCommand]
    private void AddControl()
    {
        Controls.Add(new ControlRowViewModel { Title = "Maatregel" });
    }

    [RelayCommand]
    private void RemoveControl(ControlRowViewModel? row)
    {
        if (row == null) return;
        Controls.Remove(row);
    }

    [RelayCommand]
    private void AddComponent()
    {
        Components.Add(new ComponentRowViewModel { Name = "Nieuw component", Tag = "api" });
        RefreshDiagram();
    }

    [RelayCommand]
    private void RemoveComponent(ComponentRowViewModel? row)
    {
        if (row == null) return;
        Components.Remove(row);
        foreach (var f in DataFlows.Where(f => f.From == row || f.To == row).ToList())
            DataFlows.Remove(f);
        RefreshDiagram();
    }

    [RelayCommand]
    private void AddDataFlow()
    {
        DataFlows.Add(new DataFlowRowViewModel
        {
            From = Components.FirstOrDefault(),
            To = Components.Skip(1).FirstOrDefault(),
            Label = "Data"
        });
        RefreshDiagram();
    }

    [RelayCommand]
    private void RemoveDataFlow(DataFlowRowViewModel? row)
    {
        if (row == null) return;
        DataFlows.Remove(row);
        RefreshDiagram();
    }

    [RelayCommand]
    private void AddRole()
    {
        Roles.Add(new RoleRowViewModel { Name = "Rol", Description = "" });
    }

    [RelayCommand]
    private void RemoveRole(RoleRowViewModel? row)
    {
        if (row == null) return;
        Roles.Remove(row);
    }

    [RelayCommand]
    private void SelectComponentFromDiagram(int componentId)
    {
        var row = Components.FirstOrDefault(c => c.Id == componentId);
        if (row != null)
            SelectedComponent = row;
    }

    private void RefreshDiagram()
    {
        try
        {
            var m = BuildModelFromEditor();
            var layout = _diagramLayout.Layout(m);
            DiagramNodes = new ObservableCollection<DiagramNodeViewModel>(layout.Nodes.Select(n =>
                new DiagramNodeViewModel
                {
                    ComponentId = n.ComponentId,
                    Name = n.Name,
                    Tag = n.Tag,
                    X = n.X,
                    Y = n.Y,
                    IsEntryPoint = n.IsEntryPoint,
                    IsHighlighted = SelectedComponent?.Id == n.ComponentId
                }));
            DiagramLines = new ObservableCollection<DiagramLineViewModel>(layout.Edges.Select(e =>
                new DiagramLineViewModel
                {
                    X1 = e.FromX,
                    Y1 = e.FromY,
                    X2 = e.ToX,
                    Y2 = e.ToY,
                    Label = e.Label
                }));
            DiagramTrustOverlays = new ObservableCollection<TrustBoundaryOverlayViewModel>(layout.TrustOverlays
                .Select(o => new TrustBoundaryOverlayViewModel
                {
                    X = o.X,
                    Y = o.Y,
                    Width = o.Width,
                    Height = o.Height,
                    Name = o.Name,
                    Color = o.ColorHint
                }));
        }
        catch
        {
            // layout mag editor niet breken
        }
    }

    partial void OnSelectedComponentChanged(ComponentRowViewModel? value)
    {
        foreach (var n in DiagramNodes)
            n.IsHighlighted = value != null && n.ComponentId == value.Id;
    }

    private void RefreshExportPreview()
    {
        try
        {
            var m = BuildModelFromEditor();
            ExportPreview = _export.ToMarkdown(m, Threats.ToList(), Requirements.ToList());
        }
        catch (Exception ex)
        {
            ExportPreview = $"Exportvoorbeeld mislukt: {ex.Message}";
        }
    }

    private void RefreshTraceability()
    {
        try
        {
            var m = BuildModelFromEditor();
            TraceabilityText = _traceability.BuildTraceabilitySummary(m);
        }
        catch
        {
            TraceabilityText = "Kon traceability niet opbouwen.";
        }
    }

    private void RefreshFilters()
    {
        IEnumerable<ThreatModel> tq = Threats;
        if (!string.IsNullOrWhiteSpace(ThreatFilterText))
        {
            var f = ThreatFilterText.Trim();
            tq = tq.Where(t =>
                t.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                t.StrideCategory.ToString().Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        tq = ThreatSort switch
        {
            "Status" => tq.OrderBy(t => t.Status).ThenBy(t => t.Title),
            "Category" => tq.OrderBy(t => t.StrideCategory).ThenBy(t => t.Title),
            _ => tq.OrderByDescending(t => t.Severity).ThenBy(t => t.Title)
        };

        FilteredThreats = new ObservableCollection<ThreatModel>(tq);

        IEnumerable<RequirementModel> rq = Requirements;
        if (!string.IsNullOrWhiteSpace(RequirementFilterText))
        {
            var f = RequirementFilterText.Trim();
            rq = rq.Where(r =>
                r.Title.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                r.Category.Contains(f, StringComparison.OrdinalIgnoreCase) ||
                r.PlainExplanation.Contains(f, StringComparison.OrdinalIgnoreCase));
        }

        rq = RequirementSort switch
        {
            "Status" => rq.OrderBy(r => r.Status).ThenBy(r => r.Title),
            "Category" => rq.OrderBy(r => r.Category).ThenBy(r => r.Title),
            _ => rq.OrderByDescending(r => r.Priority).ThenBy(r => r.Title)
        };

        FilteredRequirements = new ObservableCollection<RequirementModel>(rq);
    }

    private void UpdateDashboard()
    {
        OpenThreatCount = Threats.Count(t => t.Status == ThreatStatus.Open);
        MitigatedThreatCount =
            Threats.Count(t => t.Status is ThreatStatus.Mitigated or ThreatStatus.Accepted);
        OpenRequirementCount = Requirements.Count(r =>
            r.Status is RequirementStatus.Proposed or RequirementStatus.Accepted);
        ImplementedRequirementCount = Requirements.Count(r => r.Status == RequirementStatus.Implemented);
    }

    [RelayCommand]
    private void ExportMarkdown()
    {
        try
        {
            var m = BuildModelFromEditor();
            var md = _export.ToMarkdown(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "Markdown (*.md)|*.md|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.md"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, md);
                StatusMessage = "Markdown geëxporteerd.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportPlainText()
    {
        try
        {
            var m = BuildModelFromEditor();
            var txt = _export.ToPlainText(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "Text (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, txt);
                StatusMessage = "Tekst geëxporteerd.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportHtml()
    {
        try
        {
            var m = BuildModelFromEditor();
            var html = _export.ToHtml(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "HTML (*.html)|*.html|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.html"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, html);
                StatusMessage = "HTML geëxporteerd.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportStructuredJson()
    {
        try
        {
            var m = BuildModelFromEditor();
            var json = _export.ToStructuredJson(m, Threats.ToList(), Requirements.ToList());
            var dlg = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"{SanitizeFileName(m.Name)}-designguard.json"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, json);
                StatusMessage = "JSON geëxporteerd.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export mislukt: {ex.Message}";
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "project" : name.Trim();
    }
}

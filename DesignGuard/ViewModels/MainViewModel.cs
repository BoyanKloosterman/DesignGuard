// Kern: DI, state properties, navigatie-hooks (partial MainViewModel).
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using DesignGuard.Configuration;
using DesignGuard.Data.Mongo;
using DesignGuard.Export;
using DesignGuard.Knowledge;
using DesignGuard.Models;
using DesignGuard.Services;
using DesignGuard.Settings;
using DesignGuard.Theming;

namespace DesignGuard.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly JsonSerializerOptions SnapshotJsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IProjectRepository _projects;
    private readonly ThreatGenerationService _threatService;
    private readonly RequirementGenerationService _requirementService;
    private readonly DiagramLayoutService _diagramLayout;
    private readonly ExportService _export;
    private readonly AnalysisMergeService _merge;
    private readonly TraceabilityService _traceability;
    private readonly ProjectTemplateService _templates;
    private readonly ControlLibraryService _controlLibrary;
    private readonly ModelingSuggestionService _suggestionService;
    private readonly KnowledgePackService _knowledgePacks;
    private readonly KnowledgePackRemoteSyncService _packRemoteSync;
    private readonly UserSettingsService _userSettings;
    private readonly PdfReportService _pdfReport;
    private readonly DiagramRasterizer _diagramRasterizer;
    private readonly C4ModelRasterizer _c4Rasterizer;
    private readonly AppSecurityReviewService _appSecurityReview;
    private readonly IAppConfigurationService _appConfiguration;
    private readonly IMongoDiagnosticsService _mongoDiagnostics;
    private readonly DesignValidationService _designValidation;
    private HashSet<string> _dismissedSuggestionKeys = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _filterDebounceTimer;
    private bool _suppressPreferencePersist;
    private ObservableCollection<ThreatModel>? _threatsWatchedForPicker;

    public MainViewModel(
        IProjectRepository projects,
        ThreatGenerationService threatService,
        RequirementGenerationService requirementService,
        DiagramLayoutService diagramLayout,
        ExportService export,
        AnalysisMergeService merge,
        TraceabilityService traceability,
        ProjectTemplateService templates,
        ControlLibraryService controlLibrary,
        ModelingSuggestionService suggestionService,
        KnowledgePackService knowledgePacks,
        KnowledgePackRemoteSyncService packRemoteSync,
        UserSettingsService userSettings,
        PdfReportService pdfReport,
        DiagramRasterizer diagramRasterizer,
        C4ModelRasterizer c4Rasterizer,
        AppSecurityReviewService appSecurityReview,
        IAppConfigurationService appConfiguration,
        IMongoDiagnosticsService mongoDiagnostics,
        DesignValidationService designValidation)
    {
        _projects = projects;
        _threatService = threatService;
        _requirementService = requirementService;
        _diagramLayout = diagramLayout;
        _export = export;
        _merge = merge;
        _traceability = traceability;
        _templates = templates;
        _controlLibrary = controlLibrary;
        _suggestionService = suggestionService;
        _knowledgePacks = knowledgePacks;
        _packRemoteSync = packRemoteSync;
        _userSettings = userSettings;
        _pdfReport = pdfReport;
        _diagramRasterizer = diagramRasterizer;
        _c4Rasterizer = c4Rasterizer;
        _appSecurityReview = appSecurityReview;
        _appConfiguration = appConfiguration;
        _mongoDiagnostics = mongoDiagnostics;
        _designValidation = designValidation;
        _suppressPreferencePersist = true;
        UiTheme = string.IsNullOrWhiteSpace(_userSettings.Current.Theme) ? "Light" : _userSettings.Current.Theme;
        DetailLevel = string.IsNullOrWhiteSpace(_userSettings.Current.DetailLevel)
            ? "Beginner"
            : _userSettings.Current.DetailLevel;
        UiDensity = string.IsNullOrWhiteSpace(_userSettings.Current.UiDensity)
            ? "Comfortable"
            : _userSettings.Current.UiDensity;
        KnowledgePackManifestUrl = _userSettings.Current.KnowledgePackManifestUrl ?? "";
        KnowledgePackRemoteSyncEnabled = _userSettings.Current.KnowledgePackRemoteSyncEnabled;
        KnowledgePackSyncOnStartup = _userSettings.Current.KnowledgePackSyncOnStartup;
        KnowledgePackSyncTrustedHostExtra = _userSettings.Current.KnowledgePackSyncTrustedHostExtra ?? "";
        ReviewerDisplayName = _userSettings.Current.ReviewerDisplayName ?? "";
        ThemeSwitcher.ApplyTheme(UiTheme);
        ApplyUiDensity();
        _suppressPreferencePersist = false;
        _filterDebounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _filterDebounceTimer.Tick += (_, _) =>
        {
            _filterDebounceTimer.Stop();
            RefreshFilters();
        };
        SystemTypeOptions = Enum.GetNames(typeof(SystemType)).ToList();
        DeploymentContextOptions = Enum.GetNames(typeof(DeploymentContext)).ToList();
        ThreatStatusOptions = Enum.GetNames(typeof(ThreatStatus)).ToList();
        SeverityOptions = Enum.GetNames(typeof(SeverityEstimate)).ToList();
        RequirementStatusOptions = Enum.GetNames(typeof(RequirementStatus)).ToList();
        PriorityOptions = Enum.GetNames(typeof(RequirementPriority)).ToList();
        DesignNoteKindOptions = Enum.GetNames(typeof(DesignNoteKind)).ToList();
        FilteredThreats = new ObservableCollection<ThreatModel>();
        FilteredRequirements = new ObservableCollection<RequirementModel>();
        Suggestions = new ObservableCollection<ModelingSuggestion>();
        KnowledgePackRows = new ObservableCollection<KnowledgePackToggleRow>();
        AppSecurityReviewRows = new ObservableCollection<AppSecurityReviewRowViewModel>();
        Components.CollectionChanged += OnComponentsCollectionChanged;
        RefreshComponentTagSuggestions();
        Controls.CollectionChanged += (_, _) => RefreshControlSourceTagSuggestions();
        ControlLibraryPickList.Add(new LibraryPickItem("", "Geen bibliotheek-item"));
        foreach (var lib in _controlLibrary.EnumerateLibraryDefinitions())
            ControlLibraryPickList.Add(new LibraryPickItem(lib.Id, lib.Title));
        _threatsWatchedForPicker = Threats;
        Threats.CollectionChanged += OnThreatsCollectionChangedForControlPickers;
        RefreshControlThreatPickList();
    }

    private void OnThreatsCollectionChangedForControlPickers(object? _, NotifyCollectionChangedEventArgs __) =>
        RefreshControlThreatPickList();

    private void RefreshControlThreatPickList()
    {
        ControlThreatPickList.Clear();
        ControlThreatPickList.Add(new ThreatPickItem("", "Geen gekoppelde dreiging"));
        foreach (var t in Threats.OrderBy(x => x.Title))
            ControlThreatPickList.Add(new ThreatPickItem(t.Id, t.Title));
    }

    public IReadOnlyList<string> SystemTypeOptions { get; }
    public IReadOnlyList<string> DeploymentContextOptions { get; }
    public IReadOnlyList<string> ThreatStatusOptions { get; }
    public IReadOnlyList<string> SeverityOptions { get; }
    public IReadOnlyList<string> RequirementStatusOptions { get; }
    public IReadOnlyList<string> PriorityOptions { get; }
    public IReadOnlyList<string> DesignNoteKindOptions { get; }
    public IReadOnlyList<ProjectTemplateItem> TemplateList => _templates.ListTemplates();

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

    public IReadOnlyList<ControlLifecycleStatus> AllControlLifecycleStatuses { get; } =
        Enum.GetValues(typeof(ControlLifecycleStatus)).Cast<ControlLifecycleStatus>().ToArray();

    public IReadOnlyList<ReviewWorkflowStatus> AllReviewWorkflowStatuses { get; } =
        Enum.GetValues(typeof(ReviewWorkflowStatus)).Cast<ReviewWorkflowStatus>().ToArray();

    public IReadOnlyList<ReviewSubjectKind> AllReviewSubjectKinds { get; } =
        Enum.GetValues(typeof(ReviewSubjectKind)).Cast<ReviewSubjectKind>().ToArray();

    public IReadOnlyList<string> ReviewSubjectKindOptions { get; } =
        Enum.GetNames(typeof(ReviewSubjectKind)).ToList();

    public IReadOnlyList<string> ReviewWorkflowStatusOptions { get; } =
        Enum.GetNames(typeof(ReviewWorkflowStatus)).ToList();

    public IReadOnlyList<string> ControlLifecycleStatusOptions { get; } =
        Enum.GetNames(typeof(ControlLifecycleStatus)).ToList();

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { "Light", "Dark" };

    public IReadOnlyList<string> DetailLevelOptions { get; } = new[] { "Beginner", "Advanced" };

    public IReadOnlyList<string> UiDensityOptions { get; } = new[] { "Comfortable", "Compact" };

    public IReadOnlyList<string> ThreatSortOptions { get; } = new[] { "Severity", "Status", "Category" };

    public IReadOnlyList<string> RequirementSortOptions { get; } = new[] { "Priority", "Status", "Category" };

    public IReadOnlyList<string> PresetAssetClassifications { get; } =
        Enum.GetNames(typeof(AssetClassification));

    public IReadOnlyList<string> PresetDataSensitivityLabels { get; } =
        Enum.GetNames(typeof(DataSensitivity));

    public ObservableCollection<string> ComponentTagSuggestions { get; } = new();

    public ObservableCollection<string> ControlSourceTagSuggestions { get; } = new();

    public ObservableCollection<ThreatPickItem> ControlThreatPickList { get; } = new();

    public ObservableCollection<LibraryPickItem> ControlLibraryPickList { get; } = new();

    [ObservableProperty] private ObservableCollection<ProjectSummaryItem> _projectList = new();

    [ObservableProperty] private ProjectSummaryItem? _selectedProjectSummary;

    /// <summary>0 Dashboard, 1 Ontwerp, 2 Dreigingen, 3 Eisen, 4 Controls, 5 Beslissingen, 6 Review, 7 Traceability, 8 Export, 9 Instellingen (Mongo-diagnose), 10 App security review</summary>
    [ObservableProperty] private int _navSection;

    [ObservableProperty] private string _statusMessage = "DesignGuard v6 — klaar.";

    /// <summary>Pad van de laatste geslaagde bestandsexport (openen in Verkenner).</summary>
    [ObservableProperty] private string? _lastExportedFilePath;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _busyMessage = "";

    [ObservableProperty] private string _uiTheme = "Light";

    [ObservableProperty] private string _detailLevel = "Beginner";

    [ObservableProperty] private string _uiDensity = "Comfortable";

    [ObservableProperty] private string _mongoDiagEnvironment = "";

    [ObservableProperty] private string _mongoDiagEnvVars = "";

    [ObservableProperty] private string _mongoDiagDatabase = "";

    [ObservableProperty] private string _mongoDiagMaskedConnection = "";

    [ObservableProperty] private string _mongoDiagAppName = "";

    [ObservableProperty] private string _mongoDiagOptions = "";

    [ObservableProperty] private string _mongoDiagWarning = "";

    [ObservableProperty] private bool _mongoDiagHasConfigWarning;

    [ObservableProperty] private string _mongoDiagPing = "";

    [ObservableProperty] private bool _mongoDiagFullyConfigured;

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

    [ObservableProperty] private string _editorGovernanceSecurityOwner = "";

    [ObservableProperty] private string _editorGovernanceTechnicalOwner = "";

    [ObservableProperty] private string _editorGovernanceComplianceStakeholder = "";

    [ObservableProperty] private string _editorGovernanceReviewCadence = "";

    [ObservableProperty] private ObservableCollection<TrustBoundaryRowViewModel> _trustBoundaries = new();

    [ObservableProperty] private ObservableCollection<ComponentRowViewModel> _components = new();

    [ObservableProperty] private ObservableCollection<DataFlowRowViewModel> _dataFlows = new();

    [ObservableProperty] private ObservableCollection<RoleRowViewModel> _roles = new();

    [ObservableProperty] private ObservableCollection<AssetRowViewModel> _assets = new();

    [ObservableProperty] private ObservableCollection<DesignNoteRowViewModel> _designNotes = new();

    [ObservableProperty] private ObservableCollection<ControlRowViewModel> _controls = new();

    [ObservableProperty] private ObservableCollection<SensitiveDataRowViewModel> _sensitiveDataRows = new();

    [ObservableProperty] private ObservableCollection<ReviewItemRowViewModel> _reviewItems = new();

    [ObservableProperty] private ObservableCollection<SnapshotRowViewModel> _snapshots = new();

    [ObservableProperty] private ObservableCollection<C4ElementRowViewModel> _c4Elements = new();

    [ObservableProperty] private ObservableCollection<C4ElementRowViewModel> _c4VisualContext = new();

    [ObservableProperty] private ObservableCollection<C4ElementRowViewModel> _c4VisualContainers = new();

    [ObservableProperty] private ObservableCollection<C4ElementRowViewModel> _c4VisualComponents = new();

    [ObservableProperty] private ObservableCollection<C4ElementRowViewModel> _c4VisualCode = new();

    [ObservableProperty] private C4ElementRowViewModel? _selectedC4Element;

    [ObservableProperty] private ObservableCollection<ModelingSuggestion> _suggestions = new();

    // Mermaid-modus: de architectuur wordt als Mermaid-code aangehouden en door de WebView2 gerenderd.
    [ObservableProperty] private string _mermaidCode = string.Empty;

    [ObservableProperty] private string _mermaidSyntaxError = string.Empty;

    [ObservableProperty] private bool _hasMermaidError;

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

    [ObservableProperty] private string _validationSummaryText = "";

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

    [ObservableProperty] private ObservableCollection<KnowledgePackToggleRow> _knowledgePackRows = new();

    [ObservableProperty] private string _knowledgePackManifestUrl = "";

    [ObservableProperty] private bool _knowledgePackRemoteSyncEnabled;

    [ObservableProperty] private bool _knowledgePackSyncOnStartup;

    [ObservableProperty] private string _knowledgePackSyncTrustedHostExtra = "";

    [ObservableProperty] private string _reviewerDisplayName = "";

    [ObservableProperty] private ObservableCollection<AppSecurityReviewRowViewModel> _appSecurityReviewRows = new();

    public bool ShowProjectOverviewInDetails =>
        SelectedThreat == null && SelectedRequirement == null && SelectedComponent == null;

    public bool HasOpenProject => CurrentProjectId != 0;

    public bool HasNoProject => CurrentProjectId == 0;

    public bool IsAdvancedDetail =>
        string.Equals(DetailLevel, "Advanced", StringComparison.OrdinalIgnoreCase);

    partial void OnCurrentProjectIdChanged(int value)
    {
        OnPropertyChanged(nameof(HasOpenProject));
        OnPropertyChanged(nameof(HasNoProject));
    }

    partial void OnDetailLevelChanged(string value)
    {
        OnPropertyChanged(nameof(IsAdvancedDetail));
        PersistUserPreferences();
    }

    partial void OnUiThemeChanged(string value)
    {
        ThemeSwitcher.ApplyTheme(value);
        PersistUserPreferences();
    }

    partial void OnUiDensityChanged(string value)
    {
        ApplyUiDensity();
        PersistUserPreferences();
    }

    partial void OnReviewerDisplayNameChanged(string value) => PersistUserPreferences();

    private void PersistUserPreferences()
    {
        if (_suppressPreferencePersist) return;
        _userSettings.Current.Theme = UiTheme;
        _userSettings.Current.DetailLevel = DetailLevel;
        _userSettings.Current.UiDensity = UiDensity;
        _userSettings.Current.ReviewerDisplayName = (ReviewerDisplayName ?? "").Trim();
        _userSettings.Save();
    }

    partial void OnKnowledgePackManifestUrlChanged(string value) => PersistKnowledgePackSyncPreferences();

    partial void OnKnowledgePackRemoteSyncEnabledChanged(bool value) => PersistKnowledgePackSyncPreferences();

    partial void OnKnowledgePackSyncOnStartupChanged(bool value) => PersistKnowledgePackSyncPreferences();

    partial void OnKnowledgePackSyncTrustedHostExtraChanged(string value) => PersistKnowledgePackSyncPreferences();

    private void PersistKnowledgePackSyncPreferences()
    {
        if (_suppressPreferencePersist) return;
        _userSettings.Current.KnowledgePackManifestUrl = (KnowledgePackManifestUrl ?? "").Trim();
        _userSettings.Current.KnowledgePackRemoteSyncEnabled = KnowledgePackRemoteSyncEnabled;
        _userSettings.Current.KnowledgePackSyncOnStartup = KnowledgePackSyncOnStartup;
        _userSettings.Current.KnowledgePackSyncTrustedHostExtra = (KnowledgePackSyncTrustedHostExtra ?? "").Trim();
        _userSettings.Save();
    }

    private void ApplyUiDensity()
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        var compact = string.Equals(UiDensity, "Compact", StringComparison.OrdinalIgnoreCase);
        app.Resources["DgThickness.PageMargin"] =
            compact ? new System.Windows.Thickness(14, 10, 14, 10) : new System.Windows.Thickness(20, 16, 20, 16);
        app.Resources["DgThickness.SidebarPad"] =
            compact ? new System.Windows.Thickness(8, 10, 6, 10) : new System.Windows.Thickness(12, 14, 10, 14);
        app.Resources["DgThickness.CardPadding"] =
            compact ? new System.Windows.Thickness(10, 8, 10, 8) : new System.Windows.Thickness(14, 12, 14, 12);
    }

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
        if (value == 3) RefreshC4ThreatLinkCounts();
        if (value is 2 or 4)
        {
            RefreshFilters();
            UpdateDashboard();
        }

        if (value == 8) RefreshTraceability();
        if (value == 9) RefreshExportPreview();
        if (value == 10)
        {
            RefreshKnowledgePackRows();
            RefreshMongoDiagnostics();
        }
        if (value == 11) RefreshAppSecurityReview();
        if (value is 0 or 1 or 3 or 4 or 5 or 6) RefreshSuggestions();
    }

    partial void OnSelectedThreatChanged(ThreatModel? value)
    {
        if (NavSection == 1) RefreshDiagram();
    }

    partial void OnThreatFilterTextChanged(string value)
    {
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    partial void OnRequirementFilterTextChanged(string value)
    {
        _filterDebounceTimer.Stop();
        _filterDebounceTimer.Start();
    }

    partial void OnThreatSortChanged(string value) => RefreshFilters();

    partial void OnRequirementSortChanged(string value) => RefreshFilters();

    private void OnComponentsCollectionChanged(object? _, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ComponentRowViewModel c in e.NewItems)
            {
                c.PropertyChanged += ComponentRowTagPropertyChanged;
                EnsureComponentTagSuggestion(c.Tag);
            }
        }

        if (e.OldItems != null)
        {
            foreach (ComponentRowViewModel c in e.OldItems)
                c.PropertyChanged -= ComponentRowTagPropertyChanged;
        }

        // Reset levert geen OldItems: handlers loskoppelen gebeurt vóór Clear() in Project.cs.
        if (e.Action == NotifyCollectionChangedAction.Reset)
            RefreshComponentTagSuggestions();
        RefreshAssetComponentPicks();
    }

    private void RefreshAssetComponentPicks()
    {
        foreach (var a in Assets)
            a.RebuildComponentPicks(Components);
    }

    private void ComponentRowTagPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ComponentRowViewModel.Tag) && sender is ComponentRowViewModel c)
            EnsureComponentTagSuggestion(c.Tag);
    }

    /// <summary>Voegt één tag toe aan suggesties zonder ItemsSource te resetten (voorkomt leeggemaakte ComboBox-bindings).</summary>
    private void EnsureComponentTagSuggestion(string? tag)
    {
        var t = tag?.Trim();
        if (string.IsNullOrEmpty(t)) return;
        foreach (var x in ComponentTagSuggestions)
        {
            if (string.Equals(x, t, StringComparison.OrdinalIgnoreCase))
                return;
        }

        ComponentTagSuggestions.Add(t);
    }

    /// <summary>Volledige herbouw (alleen na project laden of na Components.Clear).</summary>
    private void RefreshComponentTagSuggestions()
    {
        ComponentTagSuggestions.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in DesignDropdownPresets.ComponentTags)
            if (seen.Add(t)) ComponentTagSuggestions.Add(t);
        foreach (var c in Components)
        {
            var tag = c.Tag?.Trim();
            if (!string.IsNullOrEmpty(tag) && seen.Add(tag)) ComponentTagSuggestions.Add(tag);
        }
    }

    private void DetachComponentRowTagSuggestionHandlers()
    {
        foreach (var c in Components)
            c.PropertyChanged -= ComponentRowTagPropertyChanged;
    }

    private void RefreshControlSourceTagSuggestions()
    {
        ControlSourceTagSuggestions.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in DesignDropdownPresets.ControlSourceTags)
            if (seen.Add(t)) ControlSourceTagSuggestions.Add(t);
        foreach (var row in Controls)
        {
            foreach (var token in SplitCommaList(row.SourceTags))
                if (seen.Add(token)) ControlSourceTagSuggestions.Add(token);
        }
    }

    partial void OnThreatsChanged(ObservableCollection<ThreatModel> value)
    {
        if (_threatsWatchedForPicker != null)
            _threatsWatchedForPicker.CollectionChanged -= OnThreatsCollectionChangedForControlPickers;
        _threatsWatchedForPicker = value;
        value.CollectionChanged += OnThreatsCollectionChangedForControlPickers;
        RefreshControlThreatPickList();
    }
}

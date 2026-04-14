using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Export;
using DesignGuard.Models;
using DesignGuard.Rules.RequirementRules;
using DesignGuard.Rules.ThreatRules;
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

    public MainViewModel(
        IProjectRepository projects,
        ThreatGenerationService threatService,
        RequirementGenerationService requirementService,
        DiagramLayoutService diagramLayout,
        ExportService export)
    {
        _projects = projects;
        _threatService = threatService;
        _requirementService = requirementService;
        _diagramLayout = diagramLayout;
        _export = export;
        SystemTypeOptions = Enum.GetNames(typeof(SystemType)).ToList();
    }

    public IReadOnlyList<string> SystemTypeOptions { get; }

    [ObservableProperty] private ObservableCollection<ProjectSummaryItem> _projectList = new();

    [ObservableProperty] private ProjectSummaryItem? _selectedProjectSummary;

    [ObservableProperty] private int _mainTabIndex;

    [ObservableProperty] private string _statusMessage = "Welkom bij DesignGuard.";

    [ObservableProperty] private int _currentProjectId;

    [ObservableProperty] private string _editorProjectName = "";

    [ObservableProperty] private string _editorProjectDescription = "";

    [ObservableProperty] private string _editorSystemName = "";

    [ObservableProperty] private string _editorSystemType = SystemType.WebApp.ToString();

    [ObservableProperty] private bool _flagPersonalData;

    [ObservableProperty] private bool _flagAuth;

    [ObservableProperty] private bool _flagAdmin;

    [ObservableProperty] private bool _flagExternalApi;

    [ObservableProperty] private bool _flagUpload;

    [ObservableProperty] private bool _flagSensitiveStorage;

    [ObservableProperty] private ObservableCollection<ComponentRowViewModel> _components = new();

    [ObservableProperty] private ObservableCollection<DataFlowRowViewModel> _dataFlows = new();

    [ObservableProperty] private ObservableCollection<RoleRowViewModel> _roles = new();

    [ObservableProperty] private ObservableCollection<DiagramNodeViewModel> _diagramNodes = new();

    [ObservableProperty] private ObservableCollection<DiagramLineViewModel> _diagramLines = new();

    [ObservableProperty] private ObservableCollection<ThreatModel> _threats = new();

    [ObservableProperty] private ObservableCollection<RequirementModel> _requirements = new();

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

    /// <summary>Detailpaneel: project tonen als er geen rij uit lijsten gekozen is.</summary>
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

    partial void OnMainTabIndexChanged(int value)
    {
        if (value is 1 or 2) RefreshAnalysis();
        if (value == 3) RefreshExportPreview();
        if (value == 0) RefreshDiagram();
    }

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
            StatusMessage = "Database gereed. Demo-project beschikbaar.";
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
            RefreshAnalysis();
            RefreshDiagram();
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
        FlagPersonalData = FlagAuth = FlagAdmin = FlagExternalApi = FlagUpload = FlagSensitiveStorage = false;
        Components.Clear();
        DataFlows.Clear();
        Roles.Clear();
        Threats.Clear();
        Requirements.Clear();
        DiagramNodes.Clear();
        DiagramLines.Clear();
        ExportPreview = "";
        SelectedThreat = null;
        SelectedRequirement = null;
        SelectedComponent = null;
    }

    private void ApplyModelToEditor(ProjectModel p)
    {
        CurrentProjectId = p.Id;
        EditorProjectName = p.Name;
        EditorProjectDescription = p.Description;
        EditorSystemName = p.SystemName;
        EditorSystemType = p.SystemType.ToString();
        FlagPersonalData = p.PersonalDataProcessed;
        FlagAuth = p.HasAuthentication;
        FlagAdmin = p.HasAdmin;
        FlagExternalApi = p.ExternalApis;
        FlagUpload = p.FileUpload;
        FlagSensitiveStorage = p.SensitiveDataStored;

        Components.Clear();
        foreach (var c in p.Components)
        {
            Components.Add(new ComponentRowViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
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
    }

    private ProjectModel BuildModelFromEditor()
    {
        if (!Enum.TryParse<SystemType>(EditorSystemType, out var st))
            st = SystemType.WebApp;

        var compList = Components.Select(c => new ComponentModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Tag = c.Tag
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

        return new ProjectModel
        {
            Id = CurrentProjectId,
            Name = EditorProjectName,
            Description = EditorProjectDescription,
            SystemName = EditorSystemName,
            SystemType = st,
            PersonalDataProcessed = FlagPersonalData,
            HasAuthentication = FlagAuth,
            HasAdmin = FlagAdmin,
            ExternalApis = FlagExternalApi,
            FileUpload = FlagUpload,
            SensitiveDataStored = FlagSensitiveStorage,
            Components = compList,
            DataFlows = flows,
            UserRoles = roleList
        };
    }

    [RelayCommand]
    private void NewProject()
    {
        SelectedProjectSummary = null;
        ClearEditor();
        EditorProjectName = "Nieuw project";
        EditorSystemName = "Mijn systeem";
        StatusMessage = "Nieuw project — vul ontwerp in en sla op.";
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
            RefreshAnalysis();
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
    private void AddComponent()
    {
        Components.Add(new ComponentRowViewModel
        {
            Name = "Nieuw component",
            Tag = "api"
        });
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
    private void RefreshAnalysis()
    {
        try
        {
            var prevThreat = SelectedThreat;
            var prevReq = SelectedRequirement;
            var m = BuildModelFromEditor();
            var t = _threatService.Generate(m);
            var r = _requirementService.Generate(m);
            Threats = new ObservableCollection<ThreatModel>(t);
            Requirements = new ObservableCollection<RequirementModel>(r);
            // Zelfde item weer kiezen na regeneratie (nieuwe objecten/Id's).
            SelectedThreat = prevThreat == null
                ? null
                : Threats.FirstOrDefault(x =>
                    x.Title == prevThreat.Title && x.StrideCategory == prevThreat.StrideCategory);
            SelectedRequirement = prevReq == null
                ? null
                : Requirements.FirstOrDefault(x =>
                    x.Title == prevReq.Title && x.Category == prevReq.Category);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analyse mislukt: {ex.Message}";
        }
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
                    Name = n.Name,
                    Tag = n.Tag,
                    X = n.X,
                    Y = n.Y
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
        }
        catch
        {
            // layout mag editor niet breken
        }
    }

    private void RefreshExportPreview()
    {
        try
        {
            RefreshAnalysis();
            var m = BuildModelFromEditor();
            ExportPreview = _export.ToMarkdown(m, Threats.ToList(), Requirements.ToList());
        }
        catch (Exception ex)
        {
            ExportPreview = $"Exportvoorbeeld mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ExportMarkdown()
    {
        try
        {
            var m = BuildModelFromEditor();
            var t = _threatService.Generate(m);
            var r = _requirementService.Generate(m);
            var md = _export.ToMarkdown(m, t, r);
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
            var t = _threatService.Generate(m);
            var r = _requirementService.Generate(m);
            var txt = _export.ToPlainText(m, t, r);
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

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return string.IsNullOrWhiteSpace(name) ? "project" : name.Trim();
    }
}

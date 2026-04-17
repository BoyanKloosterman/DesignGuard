// Levenscyclus project: laden, opslaan, editor in/uit model, sjablonen.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;
using DesignGuard.Services;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private async Task InitializeAsync()
    {
        RefreshMongoDiagnostics();
        try
        {
            if (!_appConfiguration.Current.IsMongoFullyConfigured)
            {
                await ReloadProjectListAsync();
                StatusMessage = _appConfiguration.Current.ConfigurationWarning ??
                                "MongoDB niet geconfigureerd — zie Instellingen.";
                RefreshKnowledgePackRows();
                var earlyPackSync = await TrySyncKnowledgePacksOnStartupAsync();
                if (earlyPackSync != null)
                    StatusMessage = $"{StatusMessage} — {earlyPackSync}";
                return;
            }

            await _projects.EnsureDatabaseAsync();
            await _projects.EnsureDemoProjectAsync();
            await ReloadProjectListAsync();
            var demo = ProjectList.FirstOrDefault(p =>
                           p.Name.Contains("uitgebreid", StringComparison.OrdinalIgnoreCase))
                       ?? ProjectList.FirstOrDefault(p => p.Name.StartsWith("Demo", StringComparison.Ordinal));
            SelectedProjectSummary = demo ?? ProjectList.FirstOrDefault();
            RefreshKnowledgePackRows();
            var latePackSync = await TrySyncKnowledgePacksOnStartupAsync();
            StatusMessage = latePackSync != null
                ? $"MongoDB gereed — {latePackSync}"
                : "MongoDB gereed. Demo-project beschikbaar indien aangemaakt.";
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
                await RunRegenerateAnalysisAsync(showBusyOverlay: false);
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
        EntryPoints.Clear();
        SensitiveDataRows.Clear();
        ReviewItems.Clear();
        Snapshots.Clear();
        Suggestions.Clear();
        _dismissedSuggestionKeys.Clear();
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
        _dismissedSuggestionKeys = new HashSet<string>(p.DismissedSuggestionKeys, StringComparer.Ordinal);

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
                StableId = c.StableId,
                Title = c.Title,
                Category = c.Category,
                SourceTags = string.Join(", ", c.SourceTags),
                Description = c.Description,
                ImplementationGuidance = c.ImplementationGuidance,
                LinkedThreatStableId = c.LinkedThreatStableId,
                LinkedRequirementStableIds = string.Join(", ", c.LinkedRequirementStableIds),
                Status = c.Status.ToString(),
                StatusNotes = c.StatusNotes,
                LibraryDefinitionId = c.LibraryDefinitionId
            });
        }

        EntryPoints.Clear();
        foreach (var ep in p.EntryPoints)
        {
            EntryPoints.Add(new EntryPointRowViewModel
            {
                Id = ep.Id,
                Name = ep.Name,
                Description = ep.Description,
                RelatedComponentId = ep.RelatedComponentId,
                Notes = ep.Notes,
                ExposureNotes = ep.ExposureNotes
            });
        }

        SensitiveDataRows.Clear();
        foreach (var s in p.SensitiveDataItems)
        {
            SensitiveDataRows.Add(new SensitiveDataRowViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                RelatedComponentId = s.RelatedComponentId,
                StorageLocation = s.StorageLocation,
                Notes = s.Notes
            });
        }

        ReviewItems.Clear();
        foreach (var r in p.ReviewItems)
        {
            ReviewItems.Add(new ReviewItemRowViewModel
            {
                Id = r.Id,
                SubjectKind = r.SubjectKind.ToString(),
                SubjectStableId = r.SubjectStableId,
                SubjectTitle = r.SubjectTitle,
                Status = r.Status.ToString(),
                Notes = r.Notes,
                Rationale = r.Rationale,
                Owner = r.Owner,
                CreatedAtUtc = r.CreatedAtUtc
            });
        }

        Snapshots.Clear();
        foreach (var s in p.Snapshots)
        {
            Snapshots.Add(new SnapshotRowViewModel
            {
                Id = s.Id,
                Name = s.Name,
                CreatedAtUtc = s.CreatedAtUtc,
                SnapshotJson = s.SnapshotJson
            });
        }

        Threats = new ObservableCollection<ThreatModel>(p.Threats);
        Requirements = new ObservableCollection<RequirementModel>(p.Requirements);
        RefreshFilters();
        UpdateDashboard();
        RefreshSuggestions();
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
            StableId = c.StableId,
            Title = c.Title,
            Category = c.Category,
            SourceTags = SplitCommaList(c.SourceTags),
            Description = c.Description,
            ImplementationGuidance = c.ImplementationGuidance,
            LinkedThreatStableId = c.LinkedThreatStableId,
            LinkedRequirementStableIds = SplitCommaList(c.LinkedRequirementStableIds),
            Status = Enum.TryParse<ControlLifecycleStatus>(c.Status, out var cst)
                ? cst
                : ControlLifecycleStatus.Draft,
            StatusNotes = c.StatusNotes,
            LibraryDefinitionId = c.LibraryDefinitionId
        }).ToList();

        var entryList = EntryPoints.Select(e => new EntryPointModel
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            RelatedComponentId = e.RelatedComponentId,
            Notes = e.Notes,
            ExposureNotes = e.ExposureNotes
        }).ToList();

        var sensList = SensitiveDataRows.Select(s => new SensitiveDataModel
        {
            Id = s.Id,
            Name = s.Name,
            Category = s.Category,
            Description = s.Description,
            RelatedComponentId = s.RelatedComponentId,
            StorageLocation = s.StorageLocation,
            Notes = s.Notes
        }).ToList();

        var revList = ReviewItems.Select(r => new ReviewItemModel
        {
            Id = r.Id,
            SubjectKind = Enum.TryParse<ReviewSubjectKind>(r.SubjectKind, out var sk)
                ? sk
                : ReviewSubjectKind.OpenQuestion,
            SubjectStableId = r.SubjectStableId,
            SubjectTitle = r.SubjectTitle,
            Status = Enum.TryParse<ReviewWorkflowStatus>(r.Status, out var rs)
                ? rs
                : ReviewWorkflowStatus.Draft,
            Notes = r.Notes,
            Rationale = r.Rationale,
            Owner = r.Owner,
            CreatedAtUtc = r.CreatedAtUtc
        }).ToList();

        var snapList = Snapshots.Select(s => new SnapshotModel
        {
            Id = s.Id,
            Name = s.Name,
            CreatedAtUtc = s.CreatedAtUtc,
            SnapshotJson = s.SnapshotJson
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
            EntryPoints = entryList,
            SensitiveDataItems = sensList,
            ReviewItems = revList,
            Snapshots = snapList,
            Threats = Threats.ToList(),
            Requirements = Requirements.ToList(),
            DismissedSuggestionKeys = _dismissedSuggestionKeys.ToList()
        };
    }

    private static List<string> SplitCommaList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0).ToList();
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

    /// <summary>Dreigingen/eisen herberekenen op model (zelfde logica als handmatige ververs-knop).</summary>
    private void RegenerateAnalysisOnModel(ProjectModel m)
    {
        var genT = _threatService.Generate(m);
        var genR = _requirementService.Generate(m);
        _merge.MergeThreats(m, genT);
        _merge.MergeRequirements(m, genR);
        RequirementThreatLinker.Link(m);
        _controlLibrary.ApplyRecommendations(m);
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

            await Task.Run(() => RegenerateAnalysisOnModel(m));
            var id = await _projects.SaveAsync(m);
            CurrentProjectId = id;
            await ReloadProjectListAsync();
            SelectedProjectSummary = ProjectList.FirstOrDefault(p => p.Id == id);
            ApplyModelToEditor(await _projects.GetAsync(id) ?? m);
            RefreshDiagram();
            StatusMessage = "Opgeslagen; dreigingen en eisen bijgewerkt naar het ontwerp.";
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
            var demo = ProjectList.FirstOrDefault(p =>
                           p.Name.Contains("uitgebreid", StringComparison.OrdinalIgnoreCase))
                       ?? ProjectList.FirstOrDefault(p => p.Name.StartsWith("Demo", StringComparison.Ordinal));
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
        var w = new global::DesignGuard.ProjectWizardWindow(this);
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
    private async Task RegenerateFromDesign() => await RunRegenerateAnalysisAsync(showBusyOverlay: true);

    private async Task RunRegenerateAnalysisAsync(bool showBusyOverlay)
    {
        try
        {
            var m = BuildModelFromEditor();
            if (showBusyOverlay)
            {
                IsBusy = true;
                BusyMessage = "Analyse draait op de achtergrond…";
            }

            await Task.Run(() => RegenerateAnalysisOnModel(m));

            Threats = new ObservableCollection<ThreatModel>(m.Threats);
            Requirements = new ObservableCollection<RequirementModel>(m.Requirements);
            MergeLibraryControlsIntoRows(m);
            RefreshFilters();
            UpdateDashboard();
            RefreshTraceability();
            RefreshSuggestions();
            if (NavSection == 1)
                RefreshDiagram();
            StatusMessage = "Analyse vernieuwd (samengevoegd met handmatige items).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Analyse mislukt: {ex.Message}";
        }
        finally
        {
            if (showBusyOverlay)
            {
                IsBusy = false;
                BusyMessage = "";
            }
        }
    }
}

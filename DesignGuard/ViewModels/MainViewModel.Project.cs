// Levenscyclus project: laden, opslaan, editor in/uit model, sjablonen.
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard;
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
        EditorGovernanceSecurityOwner = "";
        EditorGovernanceTechnicalOwner = "";
        EditorGovernanceComplianceStakeholder = "";
        EditorGovernanceReviewCadence = "";
        EditorAssessmentGoal = "";
        EditorAssessmentTestType = AssessmentTestType.Unspecified.ToString();
        EditorScopeIn = "";
        EditorScopeOut = "";
        EditorRulesOfEngagementNotes = "";
        _completedPlaybookItemIds.Clear();
        DetachComponentRowTagSuggestionHandlers();
        TrustBoundaries.Clear();
        Components.Clear();
        DataFlows.Clear();
        Roles.Clear();
        Assets.Clear();
        DesignNotes.Clear();
        Controls.Clear();
        SensitiveDataRows.Clear();
        ReviewItems.Clear();
        Snapshots.Clear();
        C4Elements.Clear();
        SelectedC4Element = null;
        C4Relations.Clear();
        SelectedC4Relation = null;
        Suggestions.Clear();
        _dismissedSuggestionKeys.Clear();
        _completedPlaybookItemIds.Clear();
        Threats.Clear();
        Requirements.Clear();
        MermaidCode = string.Empty;
        MermaidSyntaxError = string.Empty;
        ExportPreview = "";
        SelectedThreat = null;
        SelectedRequirement = null;
        SelectedComponent = null;
        TraceabilityText = "";
        RefreshFilters();
        UpdateDashboard();
        ClearPersistedEditorSnapshot();
    }

    private void ApplyModelToEditor(ProjectModel p)
    {
        LastExportedFilePath = null;
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
        EditorGovernanceSecurityOwner = p.GovernanceSecurityOwner;
        EditorGovernanceTechnicalOwner = p.GovernanceTechnicalOwner;
        EditorGovernanceComplianceStakeholder = p.GovernanceComplianceStakeholder;
        EditorGovernanceReviewCadence = p.GovernanceReviewCadence;
        EditorAssessmentGoal = p.AssessmentGoal;
        EditorAssessmentTestType = p.AssessmentTestType.ToString();
        EditorScopeIn = p.ScopeIn;
        EditorScopeOut = p.ScopeOut;
        EditorRulesOfEngagementNotes = p.RulesOfEngagementNotes;
        _completedPlaybookItemIds = new HashSet<string>(p.CompletedPlaybookItemIds, StringComparer.Ordinal);
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

        DetachComponentRowTagSuggestionHandlers();
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
                AssetClassification = c.AssetClassification,
                DataSensitivity = c.StoresOrProcesses,
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
            a.NormalizeRelatedComponents();
            var ids = a.RelatedComponentIds;
            Assets.Add(new AssetRowViewModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Classification = a.Classification,
                Sensitivity = a.Sensitivity,
                Notes = a.Notes,
                RelatedComponent = ids.Count > 0 ? Components.FirstOrDefault(c => c.Id == ids[0]) : null,
                ExtraRelatedComponentIds = ids.Count > 1 ? string.Join(", ", ids.Skip(1)) : ""
            });
        }

        foreach (var a in Assets)
            a.RebuildComponentPicks(Components);

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
            var linkIds = c.LinkedComponentIds ?? new List<int>();
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
                LibraryDefinitionId = c.LibraryDefinitionId,
                LinkedComponent = linkIds.Count > 0
                    ? Components.FirstOrDefault(x => x.Id == linkIds[0])
                    : null,
                ExtraLinkedComponentIds = linkIds.Count > 1 ? string.Join(", ", linkIds.Skip(1)) : ""
            });
        }

        foreach (var row in Controls)
            row.RebuildLinkedRequirementChips(p.Requirements);

        // Oude projecten: entry-alleen-in-lijst → zelfde component de Entry-vlag geven.
        foreach (var ep in p.EntryPoints)
        {
            if (ep.RelatedComponentId == 0) continue;
            var row = Components.FirstOrDefault(c => c.Id == ep.RelatedComponentId);
            if (row != null)
                row.IsEntryPoint = true;
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
                RelatedComponent = Components.FirstOrDefault(c => c.Id == s.RelatedComponentId),
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

        C4Elements.Clear();
        foreach (var el in p.C4Elements.OrderBy(x => (int)x.Level).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            C4Elements.Add(new C4ElementRowViewModel
            {
                Id = el.Id,
                Level = el.Level,
                Name = el.Name,
                Description = el.Description,
                Technology = el.Technology,
                ParentId = el.ParentId
            });
        }

        C4Relations.Clear();
        foreach (var rel in p.C4Relations.OrderBy(r => r.Id))
        {
            C4Relations.Add(new C4RelationRowViewModel
            {
                Id = rel.Id,
                FromElementId = rel.FromElementId,
                ToElementId = rel.ToElementId,
                Label = rel.Label,
                LineKind = rel.LineKind
            });
        }

        Threats = new ObservableCollection<ThreatModel>(p.Threats);
        Requirements = new ObservableCollection<RequirementModel>(p.Requirements);
        RefreshC4ThreatLinkCounts();
        RefreshFilters();
        UpdateDashboard();
        RefreshSuggestions();
        RefreshControlSourceTagSuggestions();
        // Initiële Mermaid-code bouwen zodat de preview meteen iets toont bij project laden
        RefreshDiagram();
        CapturePersistedEditorSnapshot();
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
            AssetClassification = string.IsNullOrWhiteSpace(c.AssetClassification)
                ? nameof(AssetClassification.Unspecified)
                : c.AssetClassification.Trim(),
            StoresOrProcesses = string.IsNullOrWhiteSpace(c.DataSensitivity)
                ? nameof(DataSensitivity.None)
                : c.DataSensitivity.Trim(),
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

        var assetList = Assets.Select(a =>
        {
            var linkIds = ComposeLinkedComponentIds(a.RelatedComponent, a.ExtraRelatedComponentIds);
            var m = new AssetModel
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Classification = string.IsNullOrWhiteSpace(a.Classification)
                    ? nameof(AssetClassification.Unspecified)
                    : a.Classification.Trim(),
                Sensitivity = string.IsNullOrWhiteSpace(a.Sensitivity)
                    ? nameof(DataSensitivity.None)
                    : a.Sensitivity.Trim(),
                Notes = a.Notes,
                RelatedComponentIds = linkIds,
                RelatedComponentId = linkIds.Count > 0 ? linkIds[0] : 0
            };
            m.NormalizeRelatedComponents();
            return m;
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
            LibraryDefinitionId = c.LibraryDefinitionId,
            LinkedComponentIds = ComposeLinkedComponentIds(c.LinkedComponent, c.ExtraLinkedComponentIds)
        }).ToList();

        var entryList = Components
            .Where(c => c.IsEntryPoint)
            .Select(c => new EntryPointModel
            {
                Name = c.Name,
                Description = c.Description ?? "",
                RelatedComponentId = c.Id,
                Notes = c.Notes ?? "",
                ExposureNotes = ""
            })
            .ToList();

        var sensList = SensitiveDataRows.Select(s => new SensitiveDataModel
        {
            Id = s.Id,
            Name = s.Name,
            Category = s.Category,
            Description = s.Description,
            RelatedComponentId = s.RelatedComponent?.Id ?? s.RelatedComponentId,
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

        var c4List = C4Elements.Select(e => new C4ElementModel
        {
            Id = e.Id,
            Level = e.Level,
            Name = e.Name.Trim(),
            Description = e.Description.Trim(),
            Technology = e.Technology.Trim(),
            ParentId = e.ParentId
        }).ToList();

        var c4RelList = C4Relations.Select(r => new C4RelationModel
        {
            Id = r.Id,
            FromElementId = r.FromElementId,
            ToElementId = r.ToElementId,
            Label = r.Label.Trim(),
            LineKind = r.LineKind
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
            GovernanceSecurityOwner = EditorGovernanceSecurityOwner,
            GovernanceTechnicalOwner = EditorGovernanceTechnicalOwner,
            GovernanceComplianceStakeholder = EditorGovernanceComplianceStakeholder,
            GovernanceReviewCadence = EditorGovernanceReviewCadence,
            AssessmentGoal = EditorAssessmentGoal,
            AssessmentTestType = Enum.TryParse<AssessmentTestType>(EditorAssessmentTestType, out var att)
                ? att
                : AssessmentTestType.Unspecified,
            ScopeIn = EditorScopeIn,
            ScopeOut = EditorScopeOut,
            RulesOfEngagementNotes = EditorRulesOfEngagementNotes,
            CompletedPlaybookItemIds = _completedPlaybookItemIds.ToList(),
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
            C4Elements = c4List,
            C4Relations = c4RelList,
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

    private static List<int> ComposeLinkedComponentIds(ComponentRowViewModel? primary, string? extraCsv)
    {
        var ids = new List<int>();
        if (primary is { Id: > 0 })
            ids.Add(primary.Id);
        if (string.IsNullOrWhiteSpace(extraCsv)) return ids;
        foreach (var part in extraCsv.Split(new[] { ',', ';' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var id) || id <= 0) continue;
            if (!ids.Contains(id)) ids.Add(id);
        }

        return ids;
    }

    [RelayCommand]
    private void NewProject()
    {
        SelectedProjectSummary = null;
        ClearEditor();
        EditorProjectName = "Nieuw project";
        EditorSystemName = "Mijn systeem";
        NavSection = MainNavSection.Design;
        StatusMessage = "Nieuw project — gebruik de wizard of vul het ontwerp in.";
        CapturePersistedEditorSnapshot();
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
            NavSection = MainNavSection.Design;
            StatusMessage = $"Sjabloon geladen: {t.Name}. Sla op om vast te leggen.";
            CapturePersistedEditorSnapshot();
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
            if (NavSection == MainNavSection.Design)
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

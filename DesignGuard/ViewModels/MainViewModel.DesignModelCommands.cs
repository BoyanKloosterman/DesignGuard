// Ontwerprijen, suggesties, control-bibliotheek, snapshots.
using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
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

    private void MergeLibraryControlsIntoRows(ProjectModel m)
    {
        foreach (var c in m.Controls.Where(x => !string.IsNullOrWhiteSpace(x.LibraryDefinitionId)))
        {
            if (Controls.Any(r =>
                    string.Equals(r.LibraryDefinitionId, c.LibraryDefinitionId, StringComparison.OrdinalIgnoreCase)))
                continue;
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
    }

    private void RefreshSuggestions()
    {
        try
        {
            var m = BuildModelFromEditor();
            var list = _suggestionService.Evaluate(m, _dismissedSuggestionKeys);
            Suggestions = new ObservableCollection<ModelingSuggestion>(list);
        }
        catch
        {
            Suggestions.Clear();
        }
    }

    [RelayCommand]
    private void DismissSuggestion(ModelingSuggestion? s)
    {
        if (s == null || string.IsNullOrWhiteSpace(s.Key)) return;
        _dismissedSuggestionKeys.Add(s.Key);
        RefreshSuggestions();
    }

    [RelayCommand]
    private async Task ApplyControlLibrary()
    {
        try
        {
            var m = BuildModelFromEditor();
            IsBusy = true;
            BusyMessage = "Control-bibliotheek toepassen…";
            var added = await Task.Run(() => _controlLibrary.ApplyRecommendations(m));
            MergeLibraryControlsIntoRows(m);
            RefreshSuggestions();
            StatusMessage = added < 0
                ? "Control-bibliotheek: control-library.json ontbreekt of is leeg."
                : added == 0
                    ? "Control-bibliotheek: geen nieuwe matches — vul het ontwerp in en vernieuw de analyse (dreigingen/eisen)."
                    : $"Control-bibliotheek: {added} maatregel(len) toegevoegd.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Control-bibliotheek: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }

    [RelayCommand]
    private void AddEntryPointRow()
    {
        EntryPoints.Add(new EntryPointRowViewModel { Name = "Ingang" });
        RefreshSuggestions();
    }

    [RelayCommand]
    private void RemoveEntryPointRow(EntryPointRowViewModel? row)
    {
        if (row == null) return;
        EntryPoints.Remove(row);
        RefreshSuggestions();
    }

    [RelayCommand]
    private void AddSensitiveDataRow()
    {
        SensitiveDataRows.Add(new SensitiveDataRowViewModel { Name = "Dataset", Category = "PII" });
        RefreshSuggestions();
    }

    [RelayCommand]
    private void RemoveSensitiveDataRow(SensitiveDataRowViewModel? row)
    {
        if (row == null) return;
        SensitiveDataRows.Remove(row);
        RefreshSuggestions();
    }

    [RelayCommand]
    private void AddReviewItemRow()
    {
        ReviewItems.Add(new ReviewItemRowViewModel
        {
            SubjectTitle = "Review-item",
            SubjectKind = ReviewSubjectKind.OpenQuestion.ToString()
        });
    }

    [RelayCommand]
    private void RemoveReviewItemRow(ReviewItemRowViewModel? row)
    {
        if (row == null) return;
        ReviewItems.Remove(row);
    }

    [RelayCommand]
    private void SaveProjectSnapshot()
    {
        try
        {
            var m = BuildModelFromEditor();
            var backupSnaps = m.Snapshots.ToList();
            m.Snapshots = new List<SnapshotModel>();
            var json = JsonSerializer.Serialize(m, SnapshotJsonOpts);
            m.Snapshots = backupSnaps;
            Snapshots.Add(new SnapshotRowViewModel
            {
                Name = $"Snapshot {DateTime.Now:yyyy-MM-dd HH:mm}",
                CreatedAtUtc = DateTime.UtcNow,
                SnapshotJson = json
            });
            StatusMessage = "Snapshot toegevoegd. Sla het project op om vast te leggen.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Snapshot mislukt: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveSnapshotRow(SnapshotRowViewModel? row)
    {
        if (row == null) return;
        Snapshots.Remove(row);
    }
}

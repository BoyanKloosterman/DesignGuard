// Concept vs opgeslagen: vergelijk editor met laatst vastgelegde snapshot (Mongo).
using System.Text.Json;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    private static readonly JsonSerializerOptions DirtyJsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private string? _persistedEditorSnapshot;
    private DispatcherTimer _dirtyCheckTimer = null!;

    [ObservableProperty] private bool _isEditorDirty;

    /// <summary>Venstertitel: projectnaam en * bij niet-opgeslagen wijzigingen.</summary>
    public string MainWindowTitle
    {
        get
        {
            var baseTitle = string.IsNullOrWhiteSpace(EditorProjectName)
                ? "Security-by-design werkbench"
                : EditorProjectName.Trim();
            return IsEditorDirty
                ? $"DesignGuard v6 — {baseTitle} *"
                : $"DesignGuard v6 — {baseTitle}";
        }
    }

    /// <summary>Statusbalk: Mongo/concept-indicator.</summary>
    public string PersistenceStatusHint
    {
        get
        {
            if (CurrentProjectId <= 0 && string.IsNullOrWhiteSpace(EditorProjectName))
                return "Geen actief project.";
            if (CurrentProjectId <= 0)
                return "Concept — nog niet opgeslagen op MongoDB.";
            return IsEditorDirty
                ? "MongoDB: wijzigingen niet opgeslagen (gebruik Opslaan)."
                : "MongoDB: laatste wijzigingen opgeslagen.";
        }
    }

    private void InitializeDirtyTrackingTimer()
    {
        _dirtyCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        _dirtyCheckTimer.Tick += (_, _) => RecomputeEditorDirtyState();
        _dirtyCheckTimer.Start();
    }

    private void NotifyPersistenceDisplayChanged()
    {
        OnPropertyChanged(nameof(MainWindowTitle));
        OnPropertyChanged(nameof(PersistenceStatusHint));
    }

    private string SerializeEditorForDirtyCheck()
    {
        var m = BuildModelFromEditor();
        return JsonSerializer.Serialize(m, DirtyJsonOpts);
    }

    /// <summary>Na laden of succesvol opslaan: huidige staat als baseline.</summary>
    private void CapturePersistedEditorSnapshot()
    {
        try
        {
            _persistedEditorSnapshot = SerializeEditorForDirtyCheck();
            IsEditorDirty = false;
        }
        catch
        {
            _persistedEditorSnapshot = null;
            IsEditorDirty = false;
        }

        NotifyPersistenceDisplayChanged();
    }

    private void ClearPersistedEditorSnapshot()
    {
        _persistedEditorSnapshot = null;
        IsEditorDirty = false;
        NotifyPersistenceDisplayChanged();
    }

    private void RecomputeEditorDirtyState()
    {
        if (_persistedEditorSnapshot is null)
        {
            if (IsEditorDirty)
            {
                IsEditorDirty = false;
                NotifyPersistenceDisplayChanged();
            }

            return;
        }

        try
        {
            var now = SerializeEditorForDirtyCheck();
            var dirty = now != _persistedEditorSnapshot;
            if (dirty != IsEditorDirty)
            {
                IsEditorDirty = dirty;
                NotifyPersistenceDisplayChanged();
            }
        }
        catch
        {
            // negeer serialisatie-fouten tijdens tussentijdse edits
        }
    }

    partial void OnEditorProjectNameChanged(string value) => NotifyPersistenceDisplayChanged();
}

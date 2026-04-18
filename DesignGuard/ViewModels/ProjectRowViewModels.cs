using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Models;

namespace DesignGuard.ViewModels;

public partial class ProjectSummaryItem : ObservableObject
{
    public int Id { get; init; }

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private DateTime _updatedAtUtc;
}

public partial class ComponentRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _tag = "";

    [ObservableProperty] private int? _trustBoundaryId;

    [ObservableProperty] private string? _trustBoundaryName;

    [ObservableProperty] private bool _isEntryPoint;

    [ObservableProperty] private string _assetClassification =
        global::DesignGuard.Models.AssetClassification.Unspecified.ToString();

    [ObservableProperty] private string _dataSensitivity =
        global::DesignGuard.Models.DataSensitivity.None.ToString();

    [ObservableProperty] private string _notes = "";

    [ObservableProperty] private double? _visualX;

    [ObservableProperty] private double? _visualY;
}

public partial class DataFlowRowViewModel : ObservableObject
{
    [ObservableProperty] private ComponentRowViewModel? _from;

    [ObservableProperty] private ComponentRowViewModel? _to;

    [ObservableProperty] private string _label = "";

    [ObservableProperty] private string? _notes;
}

public partial class RoleRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";
}

public partial class DiagramNodeViewModel : ObservableObject
{
    [ObservableProperty] private int _componentId;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _tag = "";

    [ObservableProperty] private string _dataSensitivity = "";

    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;

    [ObservableProperty] private bool _isEntryPoint;

    [ObservableProperty] private bool _isHighlighted;

    [ObservableProperty] private bool _showSensitiveStripe;

    [ObservableProperty] private bool _isLinkedHighlight;
}

public partial class TrustBoundaryOverlayViewModel : ObservableObject
{
    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;

    [ObservableProperty] private double _width;

    [ObservableProperty] private double _height;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _color = "#3B5B8C";

    [ObservableProperty] private bool _isVisible = true;
}

public partial class DiagramLineViewModel : ObservableObject
{
    [ObservableProperty] private string _curvePath = "";

    [ObservableProperty] private string _arrowPath = "";

    [ObservableProperty] private double _labelX;

    [ObservableProperty] private double _labelY;

    [ObservableProperty] private string _label = "";
}

public partial class TrustBoundaryRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _notes = "";

    [ObservableProperty] private string _colorHint = "#4472C4";
}

public partial class AssetRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _classification =
        global::DesignGuard.Models.AssetClassification.Unspecified.ToString();

    [ObservableProperty] private string _sensitivity =
        global::DesignGuard.Models.DataSensitivity.None.ToString();

    [ObservableProperty] private string _notes = "";

    [ObservableProperty] private int _relatedComponentId;

    [ObservableProperty] private ComponentRowViewModel? _relatedComponent;

    partial void OnRelatedComponentChanged(ComponentRowViewModel? value) =>
        RelatedComponentId = value?.Id ?? 0;
}

public partial class DesignNoteRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _kind =
        global::DesignGuard.Models.DesignNoteKind.Assumption.ToString();

    [ObservableProperty] private string _title = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _notes = "";
}

public partial class ControlRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _stableId = "";

    [ObservableProperty] private string _title = "";

    [ObservableProperty] private string _category = "";

    [ObservableProperty] private string _sourceTags = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _implementationGuidance = "";

    [ObservableProperty] private string _linkedThreatStableId = "";

    [ObservableProperty] private string _linkedRequirementStableIds = "";

    [ObservableProperty] private string _status =
        global::DesignGuard.Models.ControlLifecycleStatus.Draft.ToString();

    [ObservableProperty] private string _statusNotes = "";

    [ObservableProperty] private string _libraryDefinitionId = "";

    [ObservableProperty] private ComponentRowViewModel? _linkedComponent;

    [ObservableProperty] private string _extraLinkedComponentIds = "";

    private bool _suppressChipSync;

    public ObservableCollection<ControlLinkedRequirementItem> LinkedRequirementChips { get; } = new();

    public ControlRowViewModel()
    {
        LinkedRequirementChips.CollectionChanged += (_, _) =>
        {
            if (_suppressChipSync) return;
            var joined = string.Join(", ", LinkedRequirementChips.Select(c => c.Id));
            if (joined != LinkedRequirementStableIds)
                LinkedRequirementStableIds = joined;
        };
    }

    public void RebuildLinkedRequirementChips(IEnumerable<RequirementModel> reqs)
    {
        _suppressChipSync = true;
        LinkedRequirementChips.Clear();
        foreach (var id in SplitCommaIds(LinkedRequirementStableIds))
        {
            if (string.IsNullOrWhiteSpace(id)) continue;
            var r = reqs.FirstOrDefault(x => x.Id == id);
            LinkedRequirementChips.Add(new ControlLinkedRequirementItem(r?.Title ?? id, id));
        }

        _suppressChipSync = false;
    }

    public void AddLinkedRequirement(RequirementModel r)
    {
        if (LinkedRequirementChips.Any(c => c.Id == r.Id)) return;
        LinkedRequirementChips.Add(new ControlLinkedRequirementItem(r.Title, r.Id));
    }

    private static List<string> SplitCommaIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0).ToList();
    }

    [RelayCommand]
    private void RemoveLinkedRequirementChip(ControlLinkedRequirementItem? item)
    {
        if (item == null) return;
        LinkedRequirementChips.Remove(item);
    }
}

public partial class EntryPointRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private int _relatedComponentId;

    [ObservableProperty] private ComponentRowViewModel? _relatedComponent;

    partial void OnRelatedComponentChanged(ComponentRowViewModel? value) =>
        RelatedComponentId = value?.Id ?? 0;

    [ObservableProperty] private string _notes = "";

    [ObservableProperty] private string _exposureNotes = "";
}

public partial class SensitiveDataRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _category = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private int _relatedComponentId;

    [ObservableProperty] private ComponentRowViewModel? _relatedComponent;

    partial void OnRelatedComponentChanged(ComponentRowViewModel? value) =>
        RelatedComponentId = value?.Id ?? 0;

    [ObservableProperty] private string _storageLocation = "";

    [ObservableProperty] private string _notes = "";
}

public partial class ReviewItemRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _subjectKind =
        global::DesignGuard.Models.ReviewSubjectKind.OpenQuestion.ToString();

    [ObservableProperty] private string _subjectStableId = "";

    [ObservableProperty] private string _subjectTitle = "";

    [ObservableProperty] private string _status =
        global::DesignGuard.Models.ReviewWorkflowStatus.Draft.ToString();

    [ObservableProperty] private string _notes = "";

    [ObservableProperty] private string _rationale = "";

    [ObservableProperty] private string _owner = "";

    [ObservableProperty] private DateTime _createdAtUtc = DateTime.UtcNow;
}

public partial class SnapshotRowViewModel : ObservableObject
{
    [ObservableProperty] private int _id;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private DateTime _createdAtUtc = DateTime.UtcNow;

    [ObservableProperty] private string _snapshotJson = "";
}

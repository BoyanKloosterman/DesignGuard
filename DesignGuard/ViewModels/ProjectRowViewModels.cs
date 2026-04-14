using CommunityToolkit.Mvvm.ComponentModel;
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

    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;

    [ObservableProperty] private bool _isEntryPoint;

    [ObservableProperty] private bool _isHighlighted;
}

public partial class TrustBoundaryOverlayViewModel : ObservableObject
{
    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;

    [ObservableProperty] private double _width;

    [ObservableProperty] private double _height;

    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _color = "#4472C4";
}

public partial class DiagramLineViewModel : ObservableObject
{
    [ObservableProperty] private double _x1;

    [ObservableProperty] private double _y1;

    [ObservableProperty] private double _x2;

    [ObservableProperty] private double _y2;

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

    [ObservableProperty] private string _title = "";

    [ObservableProperty] private string _description = "";

    [ObservableProperty] private string _linkedThreatStableId = "";

    [ObservableProperty] private string _statusNotes = "";
}

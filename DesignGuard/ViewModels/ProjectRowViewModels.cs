using CommunityToolkit.Mvvm.ComponentModel;

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
    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _tag = "";

    [ObservableProperty] private double _x;

    [ObservableProperty] private double _y;
}

public partial class DiagramLineViewModel : ObservableObject
{
    [ObservableProperty] private double _x1;

    [ObservableProperty] private double _y1;

    [ObservableProperty] private double _x2;

    [ObservableProperty] private double _y2;

    [ObservableProperty] private string _label = "";
}

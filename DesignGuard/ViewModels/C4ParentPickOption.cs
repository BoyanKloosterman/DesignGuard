namespace DesignGuard.ViewModels;

/// <summary>Keuze voor C4-ouder in combobox (null id = geen ouder).</summary>
public sealed class C4ParentPickOption(int? id, string label)
{
    public int? Id { get; } = id;
    public string Label { get; } = label;

    public static C4ParentPickOption None { get; } = new(null, "(geen)");

    public override string ToString() => Label;
}

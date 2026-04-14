namespace DesignGuard.Models;

public sealed class ComponentModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Vrije tag, bv. frontend, api, database, external.</summary>
    public string Tag { get; set; } = "";
}

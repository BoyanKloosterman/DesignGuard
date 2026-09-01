namespace DesignGuard.Models;

/// <summary>Gezien host/URL/API/rol. Geen credentials.</summary>
public sealed class AttackSurfaceItemModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Kind { get; set; } = "URL";
    public string Value { get; set; } = "";
    public string Notes { get; set; } = "";
}

namespace DesignGuard.Models;

/// <summary>C4-relatie voor Mermaid Rel(van, naar, label); knooppunten via C4-element-id of 0 = SysInScope (alleen C1).</summary>
public sealed class C4RelationModel
{
    public int Id { get; set; }
    public int FromElementId { get; set; }
    public int ToElementId { get; set; }
    public string Label { get; set; } = "";
}

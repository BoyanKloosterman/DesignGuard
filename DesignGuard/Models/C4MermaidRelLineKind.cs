namespace DesignGuard.Models;

/// <summary>Mermaid C4: Rel vs Rel_U/D/L/R voor minder kruisende lijnen (experimenteel in Mermaid).</summary>
public enum C4MermaidRelLineKind
{
    Default = 0,
    Up = 1,
    Down = 2,
    Left = 3,
    Right = 4
}

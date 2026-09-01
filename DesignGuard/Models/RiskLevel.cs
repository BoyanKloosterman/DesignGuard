namespace DesignGuard.Models;

/// <summary>Afgeleide risicoklasse uit kans × impact (1–25).</summary>
public enum RiskLevel
{
    Unspecified = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

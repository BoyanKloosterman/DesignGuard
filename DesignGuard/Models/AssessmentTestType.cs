namespace DesignGuard.Models;

/// <summary>Testdiepte voor de assessment-aanpak (geen live pentest-claim).</summary>
public enum AssessmentTestType
{
    Unspecified = 0,
    BlackBox = 1,
    GreyBox = 2,
    WhiteBox = 3
}

namespace DesignGuard.Models;

/// <summary>Waar het systeem primair draait — beïnvloedt dreigingsdenken.</summary>
public enum DeploymentContext
{
    OnPremises,
    Cloud,
    Hybrid,
    DesktopOnly,
    EdgeOrDevice
}

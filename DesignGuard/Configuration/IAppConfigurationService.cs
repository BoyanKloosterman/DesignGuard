namespace DesignGuard.Configuration;

public interface IAppConfigurationService
{
    AppConfiguration Current { get; }

    /// <summary>Lees opnieuw uit de omgeving (na .env-update of SetEnvironmentVariable in het proces).</summary>
    void Reload();
}

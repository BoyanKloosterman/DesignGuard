namespace DesignGuard.Configuration;

public sealed class AppConfigurationService : IAppConfigurationService
{
    public AppConfigurationService(EnvironmentConfigurationProvider provider)
    {
        Current = provider.Load();
    }

    public AppConfiguration Current { get; }
}

namespace DesignGuard.Configuration;

public sealed class AppConfigurationService : IAppConfigurationService
{
    private readonly EnvironmentConfigurationProvider _provider;

    public AppConfigurationService(EnvironmentConfigurationProvider provider)
    {
        _provider = provider;
        Current = provider.Load();
    }

    public AppConfiguration Current { get; private set; }

    public void Reload() => Current = _provider.Load();
}

namespace DesignGuard.Configuration;

public interface IAppConfigurationService
{
    AppConfiguration Current { get; }
}

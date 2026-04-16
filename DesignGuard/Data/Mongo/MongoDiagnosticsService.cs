using DesignGuard.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DesignGuard.Data.Mongo;

public sealed class MongoDiagnosticsService : IMongoDiagnosticsService
{
    private readonly IAppConfigurationService _appConfig;
    private readonly MongoConnectionFactory _factory;

    public MongoDiagnosticsService(IAppConfigurationService appConfig, MongoConnectionFactory factory)
    {
        _appConfig = appConfig;
        _factory = factory;
    }

    public MongoDiagnosticsSnapshot BuildSnapshot()
    {
        var c = _appConfig.Current;
        return new MongoDiagnosticsSnapshot
        {
            EnvironmentName = c.EnvironmentName,
            HasConnectionStringEnv = c.HasConnectionStringEnv,
            HasDatabaseEnv = c.HasDatabaseEnv,
            HasAppNameEnv = c.HasAppNameEnv,
            HasEnvironmentEnv = c.HasEnvironmentEnv,
            ConfigurationWarning = c.ConfigurationWarning,
            DatabaseName = c.MongoDatabaseName ?? "(niet ingesteld)",
            MaskedConnection = ConnectionStringMasking.MaskMongoConnection(c.MongoConnectionString),
            ApplicationName = c.MongoApplicationName,
            TimeoutSeconds = c.MongoTimeoutSeconds,
            TlsFlag = c.MongoTlsRequired,
            ReadPreference = c.MongoReadPreference,
            IsFullyConfigured = c.IsMongoFullyConfigured
        };
    }

    public async Task<MongoPingResult> PingAsync(CancellationToken ct = default)
    {
        if (!_appConfig.Current.IsMongoFullyConfigured)
        {
            return new MongoPingResult
            {
                Ok = false,
                Message = "Config onvolledig — ping overgeslagen."
            };
        }

        try
        {
            var db = _factory.GetDatabase();
            await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
            return new MongoPingResult { Ok = true, Message = "Ping OK (server bereikbaar)." };
        }
        catch (Exception ex)
        {
            return new MongoPingResult
            {
                Ok = false,
                Message = $"Ping mislukt: {ex.Message}"
            };
        }
    }
}

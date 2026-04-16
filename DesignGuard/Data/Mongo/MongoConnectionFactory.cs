using DesignGuard.Configuration;
using MongoDB.Driver;

namespace DesignGuard.Data.Mongo;

/// <summary>Maakt MongoClient met timeouts en voorkeuren uit app-config.</summary>
public sealed class MongoConnectionFactory : IDisposable
{
    private readonly IAppConfigurationService _config;
    private readonly object _lock = new();
    private MongoClient? _client;

    public MongoConnectionFactory(IAppConfigurationService config)
    {
        _config = config;
    }

    public IMongoDatabase GetDatabase()
    {
        var cfg = _config.Current;
        if (!cfg.IsMongoFullyConfigured)
            throw new InvalidOperationException(
                "MongoDB-configuratie onvolledig. Zie CONFIGURATION.md en het tabblad Instellingen.");

        lock (_lock)
        {
            _client ??= CreateClient(cfg);
            return _client.GetDatabase(cfg.MongoDatabaseName);
        }
    }

    public MongoClient GetClient()
    {
        var cfg = _config.Current;
        if (!cfg.IsMongoFullyConfigured)
            throw new InvalidOperationException("MongoDB-configuratie onvolledig.");

        lock (_lock)
        {
            _client ??= CreateClient(cfg);
            return _client;
        }
    }

    private static MongoClient CreateClient(AppConfiguration cfg)
    {
        var cs = cfg.MongoConnectionString!;
        var settings = MongoClientSettings.FromConnectionString(cs);
        if (cfg.MongoTimeoutSeconds is { } sec)
        {
            var t = TimeSpan.FromSeconds(sec);
            settings.ServerSelectionTimeout = t;
            settings.ConnectTimeout = t;
            settings.SocketTimeout = t;
        }

        if (!string.IsNullOrWhiteSpace(cfg.MongoApplicationName))
            settings.ApplicationName = cfg.MongoApplicationName;

        if (TryParseReadPreference(cfg.MongoReadPreference) is { } rp)
            settings.ReadPreference = rp;

        if (cfg.MongoTlsRequired)
            settings.UseTls = true;

        return new MongoClient(settings);
    }

    private static ReadPreference? TryParseReadPreference(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return s.Trim().ToLowerInvariant() switch
        {
            "primary" => ReadPreference.Primary,
            "primarypreferred" => ReadPreference.PrimaryPreferred,
            "secondary" => ReadPreference.Secondary,
            "secondarypreferred" => ReadPreference.SecondaryPreferred,
            "nearest" => ReadPreference.Nearest,
            _ => null
        };
    }

    public void Dispose()
    {
        // MongoClient is thread-safe en hoeft niet gesloten; geen unmanaged release nodig.
    }
}

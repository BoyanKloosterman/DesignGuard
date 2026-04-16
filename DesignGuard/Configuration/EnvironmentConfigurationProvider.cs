namespace DesignGuard.Configuration;

/// <summary>Leest DESIGNGUARD_* omgevingsvariabelen.</summary>
public sealed class EnvironmentConfigurationProvider
{
    private const string EnvConnection = "DESIGNGUARD_MONGODB_CONNECTION_STRING";
    private const string EnvDatabase = "DESIGNGUARD_MONGODB_DATABASE";
    private const string EnvAppName = "DESIGNGUARD_MONGODB_APPNAME";
    private const string EnvEnvironment = "DESIGNGUARD_ENVIRONMENT";
    private const string EnvTimeout = "DESIGNGUARD_MONGODB_TIMEOUT_SECONDS";
    private const string EnvTls = "DESIGNGUARD_MONGODB_TLS";
    private const string EnvReadPref = "DESIGNGUARD_MONGODB_READ_PREFERENCE";

    public AppConfiguration Load()
    {
        var cs = Environment.GetEnvironmentVariable(EnvConnection);
        var db = Environment.GetEnvironmentVariable(EnvDatabase);
        var app = Environment.GetEnvironmentVariable(EnvAppName);
        var env = Environment.GetEnvironmentVariable(EnvEnvironment) ?? "";
        var timeoutRaw = Environment.GetEnvironmentVariable(EnvTimeout);
        var tlsRaw = Environment.GetEnvironmentVariable(EnvTls);
        var readPref = Environment.GetEnvironmentVariable(EnvReadPref);

        int? timeout = null;
        if (!string.IsNullOrWhiteSpace(timeoutRaw) && int.TryParse(timeoutRaw.Trim(), out var t) && t > 0)
            timeout = t;

        var tls = string.Equals(tlsRaw?.Trim(), "true", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(tlsRaw?.Trim(), "1", StringComparison.OrdinalIgnoreCase);

        string? warning = null;
        if (string.IsNullOrWhiteSpace(cs) || string.IsNullOrWhiteSpace(db))
        {
            warning =
                "MongoDB is niet volledig geconfigureerd. Stel minimaal DESIGNGUARD_MONGODB_CONNECTION_STRING en " +
                "DESIGNGUARD_MONGODB_DATABASE in (omgeving of bij Development een .env naast de app). " +
                "Zie CONFIGURATION.md.";
        }

        return new AppConfiguration
        {
            EnvironmentName = env,
            MongoConnectionString = string.IsNullOrWhiteSpace(cs) ? null : cs.Trim(),
            MongoDatabaseName = string.IsNullOrWhiteSpace(db) ? null : db.Trim(),
            MongoApplicationName = string.IsNullOrWhiteSpace(app) ? null : app.Trim(),
            MongoTimeoutSeconds = timeout,
            MongoTlsRequired = tls,
            MongoReadPreference = string.IsNullOrWhiteSpace(readPref) ? null : readPref.Trim(),
            HasConnectionStringEnv = !string.IsNullOrWhiteSpace(cs),
            HasDatabaseEnv = !string.IsNullOrWhiteSpace(db),
            HasAppNameEnv = !string.IsNullOrWhiteSpace(app),
            HasEnvironmentEnv = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvEnvironment)),
            ConfigurationWarning = warning
        };
    }
}

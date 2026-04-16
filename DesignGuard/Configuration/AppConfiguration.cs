namespace DesignGuard.Configuration;

/// <summary>Snapshot van Mongo- en omgevingsinstellingen (geen logging van connection string).</summary>
public sealed class AppConfiguration
{
    public string EnvironmentName { get; init; } = "";

    /// <summary>Ruwe connection string alleen voor interne driver-gebruik; niet tonen of loggen.</summary>
    public string? MongoConnectionString { get; init; }

    public string? MongoDatabaseName { get; init; }
    public string? MongoApplicationName { get; init; }

    public int? MongoTimeoutSeconds { get; init; }

    /// <summary>Extra TLS afdwingen naast wat de connection string al aangeeft (bijv. Atlas gebruikt meestal +srv).</summary>
    public bool MongoTlsRequired { get; init; }

    public string? MongoReadPreference { get; init; }

    public bool HasConnectionStringEnv { get; init; }
    public bool HasDatabaseEnv { get; init; }
    public bool HasAppNameEnv { get; init; }
    public bool HasEnvironmentEnv { get; init; }

    public bool IsMongoFullyConfigured =>
        !string.IsNullOrWhiteSpace(MongoConnectionString) && !string.IsNullOrWhiteSpace(MongoDatabaseName);

    /// <summary>Gebruikersmelding bij ontbrekende of onvolledige configuratie.</summary>
    public string? ConfigurationWarning { get; init; }
}

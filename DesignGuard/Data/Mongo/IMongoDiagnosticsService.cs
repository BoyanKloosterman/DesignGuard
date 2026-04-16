namespace DesignGuard.Data.Mongo;

public interface IMongoDiagnosticsService
{
    MongoDiagnosticsSnapshot BuildSnapshot();

    /// <summary>Ping naar Mongo; fouttekst veilig (geen connection string).</summary>
    Task<MongoPingResult> PingAsync(CancellationToken ct = default);
}

public sealed class MongoDiagnosticsSnapshot
{
    public string EnvironmentName { get; init; } = "";
    public bool HasConnectionStringEnv { get; init; }
    public bool HasDatabaseEnv { get; init; }
    public bool HasAppNameEnv { get; init; }
    public bool HasEnvironmentEnv { get; init; }
    public string? ConfigurationWarning { get; init; }
    public string DatabaseName { get; init; } = "";
    public string MaskedConnection { get; init; } = "";
    public string? ApplicationName { get; init; }
    public int? TimeoutSeconds { get; init; }
    public bool TlsFlag { get; init; }
    public string? ReadPreference { get; init; }
    public bool IsFullyConfigured { get; init; }
}

public sealed class MongoPingResult
{
    public bool Ok { get; init; }
    public string Message { get; init; } = "";
}

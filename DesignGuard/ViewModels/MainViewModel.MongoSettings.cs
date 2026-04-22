// MongoDB-instellingen en diagnose.
using CommunityToolkit.Mvvm.Input;
using DesignGuard.Configuration;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
    private void RefreshMongoEnvEditorFromProcess()
    {
        MongoEnvRevealConnectionString = false;
        MongoEnvConnectionString =
            Environment.GetEnvironmentVariable("DESIGNGUARD_MONGODB_CONNECTION_STRING") ?? "";
        MongoEnvDatabaseName = Environment.GetEnvironmentVariable("DESIGNGUARD_MONGODB_DATABASE") ?? "";
    }

    private void RefreshMongoDiagnostics()
    {
        var s = _mongoDiagnostics.BuildSnapshot();
        MongoDiagEnvironment = string.IsNullOrWhiteSpace(s.EnvironmentName) ? "(niet gezet)" : s.EnvironmentName;
        MongoDiagEnvVars =
            $"connection string: {(s.HasConnectionStringEnv ? "gevonden" : "ontbreekt")}; database: {(s.HasDatabaseEnv ? "gevonden" : "ontbreekt")}; appName: {(s.HasAppNameEnv ? "gevonden" : "optioneel")}; omgeving: {(s.HasEnvironmentEnv ? "gevonden" : "optioneel")}";
        MongoDiagDatabase = s.DatabaseName;
        MongoDiagMaskedConnection = s.MaskedConnection;
        MongoDiagAppName = string.IsNullOrWhiteSpace(s.ApplicationName) ? "(default driver)" : s.ApplicationName!;
        var opt = new List<string>();
        if (s.TimeoutSeconds is { } t) opt.Add($"timeout {t}s");
        if (s.TlsFlag) opt.Add("TLS-flag true");
        if (!string.IsNullOrWhiteSpace(s.ReadPreference)) opt.Add($"readPreference={s.ReadPreference}");
        MongoDiagOptions = opt.Count == 0 ? "(geen optionele flags)" : string.Join(", ", opt);
        MongoDiagWarning = s.ConfigurationWarning ?? "";
        MongoDiagHasConfigWarning = !string.IsNullOrWhiteSpace(s.ConfigurationWarning);
        MongoDiagPing = "";
        MongoDiagFullyConfigured = s.IsFullyConfigured;
    }

    [RelayCommand]
    private async Task TestMongoConnectionAsync()
    {
        RefreshMongoDiagnostics();
        try
        {
            var r = await _mongoDiagnostics.PingAsync();
            MongoDiagPing = r.Message;
            StatusMessage = r.Ok ? "MongoDB ping geslaagd." : "MongoDB ping mislukt — zie Instellingen.";
        }
        catch (Exception ex)
        {
            MongoDiagPing = ex.Message;
            StatusMessage = "Ping-uitzondering — zie Instellingen.";
        }
    }

    [RelayCommand]
    private async Task SaveMongoEnvToFileAsync()
    {
        var cs = (MongoEnvConnectionString ?? "").Trim();
        var db = (MongoEnvDatabaseName ?? "").Trim();
        if (string.IsNullOrEmpty(cs) || string.IsNullOrEmpty(db))
        {
            StatusMessage = "Vul connection string en databasenaam in.";
            return;
        }

        try
        {
            await Task.Run(() => DevelopmentEnvFileWriter.SaveMongo(cs, db)).ConfigureAwait(true);
            Environment.SetEnvironmentVariable("DESIGNGUARD_MONGODB_CONNECTION_STRING", cs);
            Environment.SetEnvironmentVariable("DESIGNGUARD_MONGODB_DATABASE", db);
            Environment.SetEnvironmentVariable("DESIGNGUARD_ENVIRONMENT", "Development");
            _appConfiguration.Reload();
            _mongoConnectionFactory.ResetCachedClient();
            RefreshMongoDiagnostics();
            StatusMessage = $"MongoDB-config opgeslagen ({DevelopmentEnvFileWriter.ResolvePathForWrite()}).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Opslaan .env mislukt: {ex.Message}";
        }
    }
}

// MongoDB-instellingen, diagnose en SQLite-import.
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace DesignGuard.ViewModels;

public partial class MainViewModel
{
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
    private async Task ImportSqliteToMongoAsync()
    {
        if (!_appConfiguration.Current.IsMongoFullyConfigured)
        {
            StatusMessage = "Eerst MongoDB configureren.";
            return;
        }

        var dlg = new OpenFileDialog
        {
            Title = "SQLite DesignGuard-database (designguard-v3.db)",
            Filter = "SQLite database (*.db)|*.db|Alle bestanden (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true)
        {
            StatusMessage = "Import geannuleerd.";
            return;
        }

        try
        {
            var progress = new Progress<string>(msg => StatusMessage = msg);
            var r = await _sqliteImport.ImportAllProjectsAsync(dlg.FileName, progress);
            await ReloadProjectListAsync();
            StatusMessage =
                $"SQLite-import klaar: {r.ImportedCount}/{r.SourceProjectCount} projecten naar MongoDB.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import mislukt: {ex.Message}";
        }
    }
}

using System.IO;

namespace DesignGuard.Configuration;

/// <summary>
/// Optioneel .env: alleen als het bestand Development aangeeft, of als DESIGNGUARD_LOAD_DOTENV=1 in de process-omgeving staat.
/// Overschrijft nooit bestaande omgevingsvariabelen.
/// Zoekt ook in bovenliggende mappen (handig: .env in repo-root terwijl de exe in bin/Debug staat).
/// </summary>
public static class DevelopmentEnvFileLoader
{
    private const int MaxWalkUpLevels = 10;

    public static void TryApplyOptionalDotEnv()
    {
        foreach (var candidate in GetCandidateEnvPaths())
        {
            if (!File.Exists(candidate)) continue;
            ApplyFileIfAllowed(candidate);
            return;
        }
    }

    private static IEnumerable<string> GetCandidateEnvPaths()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < MaxWalkUpLevels && current != null; i++)
        {
            yield return Path.Combine(current.FullName, ".env");
            current = current.Parent;
        }

        yield return Path.Combine(Directory.GetCurrentDirectory(), ".env");
    }

    private static void ApplyFileIfAllowed(string path)
    {
        var vars = ParseEnvFile(path);
        var loadDotEnv = string.Equals(Environment.GetEnvironmentVariable("DESIGNGUARD_LOAD_DOTENV"), "1",
            StringComparison.Ordinal);
        vars.TryGetValue("DESIGNGUARD_ENVIRONMENT", out var fileEnv);
        var isDevInFile = string.Equals(fileEnv, "Development", StringComparison.OrdinalIgnoreCase);

        if (!loadDotEnv && !isDevInFile)
            return;

        foreach (var (key, value) in vars)
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (Environment.GetEnvironmentVariable(key) != null) continue;
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static Dictionary<string, string> ParseEnvFile(string path)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
            var eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            var key = trimmed[..eq].Trim();
            var value = trimmed[(eq + 1)..].Trim();
            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
                value = value[1..^1];
            if (value.StartsWith('\'') && value.EndsWith('\'') && value.Length >= 2)
                value = value[1..^1];
            if (string.IsNullOrEmpty(key)) continue;
            d[key] = value;
        }

        return d;
    }
}

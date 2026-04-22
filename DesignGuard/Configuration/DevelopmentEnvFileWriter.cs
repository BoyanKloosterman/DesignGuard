using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace DesignGuard.Configuration;

/// <summary>Schrijft MongoDB-keys naar .env (merge); pad volgt EnumerateCandidateDotEnvPaths.</summary>
public static class DevelopmentEnvFileWriter
{
    private const string EnvEnvironment = "DESIGNGUARD_ENVIRONMENT";
    private const string EnvConnection = "DESIGNGUARD_MONGODB_CONNECTION_STRING";
    private const string EnvDatabase = "DESIGNGUARD_MONGODB_DATABASE";

    public static string ResolvePathForWrite()
    {
        foreach (var p in DevelopmentEnvFileLoader.EnumerateCandidateDotEnvPaths())
        {
            if (File.Exists(p)) return p;
        }

        return DevelopmentEnvFileLoader.EnumerateCandidateDotEnvPaths().First();
    }

    /// <summary>Zet Development + connection string + database; overschrijft bestaande keys (case-insensitive).</summary>
    public static void SaveMongo(string connectionString, string databaseName)
    {
        var path = ResolvePathForWrite();
        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [EnvEnvironment] = FormatValue("Development"),
            [EnvConnection] = FormatValue(connectionString),
            [EnvDatabase] = FormatValue(databaseName)
        };

        var lines = File.Exists(path)
            ? File.ReadAllLines(path, Encoding.UTF8).ToList()
            : new List<string>();

        var handled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < lines.Count; i++)
        {
            if (!TryParseAssignmentKey(lines[i], out var parsedKey)) continue;
            var canon = CanonicalMongoMergeKey(parsedKey);
            if (canon == null || !updates.TryGetValue(canon, out var encoded)) continue;
            lines[i] = $"{canon}={encoded}";
            handled.Add(canon);
        }

        foreach (var canon in new[] { EnvEnvironment, EnvConnection, EnvDatabase })
        {
            if (handled.Contains(canon)) continue;
            lines.Add($"{canon}={updates[canon]}");
        }

        var dir = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        TryRestrictFileToCurrentUser(path);
    }

    /// <summary>Windows: alleen huidige gebruiker (geen erfregels) — mislukt stil bij netwerk/share.</summary>
    private static void TryRestrictFileToCurrentUser(string path)
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            var user = WindowsIdentity.GetCurrent().User;
            if (user == null) return;

            var fi = new FileInfo(path);
            var fs = fi.GetAccessControl(AccessControlSections.Access);
            fs.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            fs.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.Modify, AccessControlType.Allow));
            fi.SetAccessControl(fs);
        }
        catch
        {
            // niet blokkeren
        }
    }

    private static string? CanonicalMongoMergeKey(string parsedKey)
    {
        if (parsedKey.Equals(EnvEnvironment, StringComparison.OrdinalIgnoreCase)) return EnvEnvironment;
        if (parsedKey.Equals(EnvConnection, StringComparison.OrdinalIgnoreCase)) return EnvConnection;
        if (parsedKey.Equals(EnvDatabase, StringComparison.OrdinalIgnoreCase)) return EnvDatabase;
        return null;
    }

    private static bool TryParseAssignmentKey(string line, out string key)
    {
        key = "";
        var t = line.Trim();
        if (t.Length == 0 || t.StartsWith('#')) return false;
        var eq = t.IndexOf('=');
        if (eq <= 0) return false;
        key = t[..eq].Trim();
        return key.Length > 0;
    }

    private static bool NeedsQuoting(string s)
    {
        foreach (var c in s)
            if (c is ' ' or '\t' or '"' or '=' or '#') return true;
        return false;
    }

    private static string FormatValue(string s)
    {
        if (s.Length == 0) return "\"\"";
        if (!NeedsQuoting(s)) return s;
        return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}

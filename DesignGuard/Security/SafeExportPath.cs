using System.IO;

namespace DesignGuard.Security;

/// <summary>Minimale pad-hardening voor gebruikersexport.</summary>
public static class SafeExportPath
{
    public static bool TryGetSafeWritePath(string? userSelectedPath, out string path, out string? error)
    {
        path = "";
        error = null;
        if (string.IsNullOrWhiteSpace(userSelectedPath))
        {
            error = "Geen pad.";
            return false;
        }

        try
        {
            var full = Path.GetFullPath(userSelectedPath);
            if (full.StartsWith("\\\\?\\GLOBALROOT", StringComparison.OrdinalIgnoreCase))
            {
                error = "Ongeldig pad.";
                return false;
            }

            var dir = Path.GetDirectoryName(full);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                error = "Doelmap bestaat niet.";
                return false;
            }

            path = full;
            return true;
        }
        catch
        {
            error = "Pad kon niet worden gevalideerd.";
            return false;
        }
    }
}

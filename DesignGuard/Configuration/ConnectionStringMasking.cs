using System.Text.RegularExpressions;

namespace DesignGuard.Configuration;

public static class ConnectionStringMasking
{
    private static readonly Regex UserInfo = new(@"(//)([^/@]+)(@)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Masker userinfo in URI; host/poort blijft zichtbaar voor diagnose.</summary>
    public static string MaskMongoConnection(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(niet ingesteld)";

        try
        {
            var s = UserInfo.Replace(connectionString, "$1***$3");
            if (s.Length > 96)
                return s[..48] + "…" + s[^40..];
            return s;
        }
        catch
        {
            return "(kon connection string niet maskeren)";
        }
    }
}

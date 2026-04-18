using DesignGuard.Models;

namespace DesignGuard.Export;

/// <summary>Gedeelde C4-teksten en dreiging-koppeling (één definitie voor UI, export, PDF, raster).</summary>
public static class C4ExportPresentation
{
    public static Dictionary<int, string> BuildIdToNameMap(IEnumerable<C4ElementModel> elements) =>
        elements.ToDictionary(e => e.Id, e => string.IsNullOrWhiteSpace(e.Name) ? $"#{e.Id}" : e.Name.Trim());

    /// <summary>Aantal open dreigingen met minstens één getroffen component gelijk aan <paramref name="componentName"/> (trim, case-insensitive).</summary>
    public static int CountOpenThreatMatchesForComponentName(string? componentName, IEnumerable<ThreatModel> threats)
    {
        if (string.IsNullOrWhiteSpace(componentName)) return 0;
        var nm = componentName.Trim();
        var n = 0;
        foreach (var t in threats)
        {
            if (t.Status != ThreatStatus.Open) continue;
            foreach (var a in t.AffectedComponents)
            {
                if (string.Equals(a.Trim(), nm, StringComparison.OrdinalIgnoreCase))
                {
                    n++;
                    break;
                }
            }
        }

        return n;
    }

    public static int CountOpenThreatNameMatches(C4ElementModel el, IEnumerable<ThreatModel> threats) =>
        CountOpenThreatMatchesForComponentName(el.Name, threats);

    public static string FormatC4ParentHintPdf(C4ElementModel el, IReadOnlyDictionary<int, string> idToName)
    {
        if (el.ParentId is not { } pid) return "";
        if (idToName.TryGetValue(pid, out var label))
            return $" — ouder: {label} (id {pid})";
        return $" — ouder id: {pid} (naam niet in lijst)";
    }

    /// <summary>Korte ouderregel voor C4-kaarten (zelfde stijl als threatmodel-tab).</summary>
    public static string FormatC4ParentLabelCard(C4ElementModel el, IReadOnlyDictionary<int, string> idToName)
    {
        if (el.ParentId is not { } pid) return "";
        if (idToName.TryGetValue(pid, out var nm))
            return $"Ouder: {nm} (#{pid})";
        return $"Ouder: (onbekend #{pid})";
    }
}

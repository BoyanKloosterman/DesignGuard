namespace DesignGuard.Models;

/// <summary>Parse-hulp voor vrije tekst naast standaard enum-labels in dropdowns.</summary>
public static class DesignOntwerpWaarden
{
    /// <summary>Diagram: oranje streep bij alles behalve expliciet None.</summary>
    public static bool IsDataSensitivityVisuallyElevated(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (Enum.TryParse<DataSensitivity>(raw.Trim(), true, out var e))
            return e != DataSensitivity.None;
        return true;
    }

    public static bool IsAssetClassificationRestricted(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (Enum.TryParse<AssetClassification>(raw.Trim(), true, out var e))
            return e is AssetClassification.Confidential or AssetClassification.Restricted;
        return false;
    }

    public static bool IsAssetSensitivityElevatedForSuggestions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (Enum.TryParse<DataSensitivity>(raw.Trim(), true, out var e))
            return e != DataSensitivity.None;
        return true;
    }

    public static bool ShowsDataSensitivityInExport(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (Enum.TryParse<DataSensitivity>(raw.Trim(), true, out var e))
            return e != DataSensitivity.None;
        return true;
    }
}

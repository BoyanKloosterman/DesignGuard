using System.Text.Json;
using DesignGuard.Models;

namespace DesignGuard.Data;

internal static class JsonBlobs
{
    private static readonly JsonSerializerOptions Opts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Opts);

    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(json, Opts);
        }
        catch
        {
            return default;
        }
    }

    public static List<string> StringList(string? json) =>
        Deserialize<List<string>>(json ?? "[]") ?? new List<string>();

    public static List<int> IntList(string? json) =>
        Deserialize<List<int>>(json ?? "[]") ?? new List<int>();

    public static ExplanationModel Explanation(string? json) =>
        Deserialize<ExplanationModel>(json ?? "{}") ?? new ExplanationModel();

    public static SourceAttributionModel SourceAttribution(string? json) =>
        Deserialize<SourceAttributionModel>(json ?? "{}") ?? new SourceAttributionModel();
}

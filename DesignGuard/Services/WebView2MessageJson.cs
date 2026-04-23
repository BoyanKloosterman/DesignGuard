using System.Text.Json;

namespace DesignGuard.Services;

/// <summary>
/// Parseert WebView2 <see cref="Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs.WebMessageAsJson"/>.
/// JS gebruikt postMessage(JSON.stringify(obj)) — soms is de root een JSON-string die nog een object bevat.
/// </summary>
internal static class WebView2MessageJson
{
    public static bool TryParse(string? webMessageAsJson, out string? type, out string? message)
    {
        type = null;
        message = null;
        if (string.IsNullOrWhiteSpace(webMessageAsJson))
            return false;

        try
        {
            var obj = ParseObjectRoot(webMessageAsJson);
            if (obj.ValueKind != JsonValueKind.Object)
                return false;
            if (obj.TryGetProperty("type", out var t))
                type = t.GetString();
            if (obj.TryGetProperty("message", out var m))
                message = m.GetString();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonElement ParseObjectRoot(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.String)
        {
            var inner = root.GetString();
            if (string.IsNullOrWhiteSpace(inner))
                return default;
            using var innerDoc = JsonDocument.Parse(inner);
            return innerDoc.RootElement.Clone();
        }

        return root.Clone();
    }
}

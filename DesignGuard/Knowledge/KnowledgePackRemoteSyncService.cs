using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DesignGuard.Knowledge;

/// <summary>
/// Haalt knowledge packs binnen via HTTPS-manifest met SHA256-check.
/// Alleen hosts: manifest-host + optionele extra host; geen willekeurige URLs uit manifest.
/// </summary>
public sealed class KnowledgePackRemoteSyncService
{
    private const int MaxManifestBytes = 262_144;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HttpClient Http = CreateHttpClient();

    private readonly KnowledgePackService _packs;

    public KnowledgePackRemoteSyncService(KnowledgePackService packs)
    {
        _packs = packs;
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            CheckCertificateRevocationList = true
        };
        var c = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(90)
        };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("DesignGuard/KnowledgePackSync");
        return c;
    }

    public async Task<KnowledgePackSyncResult> SyncAsync(string manifestUrl, string? trustedHostExtra,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifestUrl))
            return new KnowledgePackSyncResult(false, "Geen manifest-URL.");

        if (!Uri.TryCreate(manifestUrl.Trim(), UriKind.Absolute, out var manifestUri) ||
            !string.Equals(manifestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return new KnowledgePackSyncResult(false, "Manifest moet een absolute HTTPS-URL zijn.");

        var allowedHosts = BuildAllowedHosts(manifestUri, trustedHostExtra);

        KnowledgePackRemoteManifestDto manifest;
        try
        {
            var (manifestBytes, finalManifestUri) =
                await DownloadLimitedAsync(Http, manifestUri, MaxManifestBytes, cancellationToken)
                    .ConfigureAwait(false);
            if (!IsFinalUriTrusted(finalManifestUri, allowedHosts))
                return new KnowledgePackSyncResult(false, "Manifest-redirect naar niet-toegestane host.");
            manifest = JsonSerializer.Deserialize<KnowledgePackRemoteManifestDto>(manifestBytes, JsonOpts)
                       ?? new KnowledgePackRemoteManifestDto();
        }
        catch (Exception ex)
        {
            return new KnowledgePackSyncResult(false, $"Manifest ophalen mislukt: {ex.Message}");
        }

        if (manifest.SchemaVersion != 1)
            return new KnowledgePackSyncResult(false, "Manifest schemaVersion moet 1 zijn.");

        if (manifest.Assets == null || manifest.Assets.Count == 0)
            return new KnowledgePackSyncResult(false, "Manifest bevat geen assets.");

        var dir = _packs.PacksDirectory;
        Directory.CreateDirectory(dir);

        var done = 0;
        foreach (var asset in manifest.Assets)
        {
            if (string.IsNullOrWhiteSpace(asset.RelativePath) ||
                asset.RelativePath.Contains("..", StringComparison.Ordinal) ||
                Path.IsPathRooted(asset.RelativePath))
                return new KnowledgePackSyncResult(false, $"Ongeldig relativePath: {asset.RelativePath}");

            if (string.IsNullOrWhiteSpace(asset.Sha256Hex) ||
                asset.Sha256Hex.Length != 64 ||
                !asset.Sha256Hex.All(Uri.IsHexDigit))
                return new KnowledgePackSyncResult(false, $"Ongeldige sha256 voor {asset.RelativePath}.");

            if (asset.SizeBytes <= 0 || asset.SizeBytes > KnowledgePackService.MaxPackFileBytes)
                return new KnowledgePackSyncResult(false,
                    $"Ongeldige grootte voor {asset.RelativePath}.");

            var sourcePart = string.IsNullOrWhiteSpace(asset.SourcePath)
                ? asset.RelativePath
                : asset.SourcePath!;

            Uri assetUri;
            try
            {
                assetUri = ResolveDownloadUri(manifestUri, sourcePart);
            }
            catch (Exception ex)
            {
                return new KnowledgePackSyncResult(false,
                    $"URL-resolutie {asset.RelativePath}: {ex.Message}");
            }

            if (!string.Equals(assetUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return new KnowledgePackSyncResult(false, $"Alleen HTTPS: {asset.RelativePath}");

            if (!allowedHosts.Contains(assetUri.Host))
                return new KnowledgePackSyncResult(false,
                    $"Host niet toegestaan voor {asset.RelativePath}: {assetUri.Host}");

            byte[] body;
            try
            {
                var (bytes, finalAssetUri) =
                    await DownloadLimitedAsync(Http, assetUri, (int)KnowledgePackService.MaxPackFileBytes,
                            cancellationToken)
                        .ConfigureAwait(false);
                if (!IsFinalUriTrusted(finalAssetUri, allowedHosts))
                    return new KnowledgePackSyncResult(false,
                        $"Download {asset.RelativePath}: redirect naar niet-toegestane host.");
                body = bytes;
            }
            catch (Exception ex)
            {
                return new KnowledgePackSyncResult(false,
                    $"Download {asset.RelativePath} mislukt: {ex.Message}");
            }

            if (body.LongLength != asset.SizeBytes)
                return new KnowledgePackSyncResult(false,
                    $"Grootte komt niet overeen: {asset.RelativePath}.");

            var hash = Convert.ToHexString(SHA256.HashData(body));
            if (!hash.Equals(asset.Sha256Hex, StringComparison.OrdinalIgnoreCase))
                return new KnowledgePackSyncResult(false,
                    $"Hash mismatch: {asset.RelativePath}.");

            if (!LooksLikeJson(body))
                return new KnowledgePackSyncResult(false,
                    $"Geen geldige JSON: {asset.RelativePath}.");

            var full = Path.GetFullPath(Path.Combine(dir, asset.RelativePath));
            if (!full.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
                return new KnowledgePackSyncResult(false, "Pad buiten KnowledgePacks.");

            var tmp = full + ".dg-sync.tmp";
            try
            {
                await File.WriteAllBytesAsync(tmp, body, cancellationToken).ConfigureAwait(false);
                if (File.Exists(full))
                    File.Delete(full);
                File.Move(tmp, full);
            }
            catch (Exception ex)
            {
                TryDelete(tmp);
                return new KnowledgePackSyncResult(false,
                    $"Schrijven {asset.RelativePath} mislukt: {ex.Message}");
            }

            done++;
        }

        return new KnowledgePackSyncResult(true,
            $"Synchronisatie gelukt: {done} bestand(en). Packs opnieuw geladen.");
    }

    private static HashSet<string> BuildAllowedHosts(Uri manifestUri, string? trustedHostExtra)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { manifestUri.Host };
        if (!string.IsNullOrWhiteSpace(trustedHostExtra))
        {
            var h = trustedHostExtra.Trim();
            if (h.Length > 0)
                set.Add(h);
        }

        return set;
    }

    /// <summary>Absolute URL: zelfde host als toegestaan door caller. Relatief: t.o.v. manifest-map.</summary>
    private static Uri ResolveDownloadUri(Uri manifestFileUri, string sourcePathOrUrl)
    {
        if (Uri.TryCreate(sourcePathOrUrl, UriKind.Absolute, out var abs))
            return abs;

        var baseForRelative = manifestFileUri.IsAbsoluteUri
            ? new Uri(manifestFileUri.GetLeftPart(UriPartial.Authority) +
                      manifestFileUri.AbsolutePath.Substring(0,
                          manifestFileUri.AbsolutePath.LastIndexOf('/') + 1), UriKind.Absolute)
            : throw new InvalidOperationException("Manifest-URI ongeldig.");

        return new Uri(baseForRelative, sourcePathOrUrl);
    }

    /// <summary>Na redirects: alleen HTTPS en host nog steeds toegestaan.</summary>
    private static bool IsFinalUriTrusted(Uri finalUri, HashSet<string> allowedHosts) =>
        string.Equals(finalUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
        allowedHosts.Contains(finalUri.Host);

    private static async Task<(byte[] Body, Uri FinalUri)> DownloadLimitedAsync(HttpClient http, Uri url, int maxBytes,
        CancellationToken cancellationToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        if (resp.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"HTTP {(int)resp.StatusCode}.");

        var finalUri = resp.RequestMessage?.RequestUri ?? url;

        var len = resp.Content.Headers.ContentLength;
        if (len.HasValue && len.Value > maxBytes)
            throw new InvalidOperationException("Response te groot (Content-Length).");

        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var ms = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                       .ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
                throw new InvalidOperationException("Response te groot tijdens lezen.");
            ms.Write(buffer, 0, read);
        }

        return (ms.ToArray(), finalUri);
    }

    private static bool LooksLikeJson(ReadOnlySpan<byte> utf8)
    {
        var s = Encoding.UTF8.GetString(utf8).TrimStart();
        return s.StartsWith('{') || s.StartsWith('[');
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // bewust leeg
        }
    }
}

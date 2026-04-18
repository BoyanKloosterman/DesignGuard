namespace DesignGuard.Knowledge;

/// <summary>Manifest voor remote sync (hash + grootte per bestand).</summary>
public sealed class KnowledgePackRemoteManifestDto
{
    public int SchemaVersion { get; set; }

    public List<KnowledgePackRemoteAssetDto> Assets { get; set; } = new();
}

public sealed class KnowledgePackRemoteAssetDto
{
    /// <summary>Bestandsnaam onder KnowledgePacks (geen pad-traversal).</summary>
    public string RelativePath { get; set; } = "";

    public string Sha256Hex { get; set; } = "";

    public long SizeBytes { get; set; }

    /// <summary>Optioneel download-pad t.o.v. manifest-URL; anders gelijk aan RelativePath.</summary>
    public string? SourcePath { get; set; }
}

public sealed record KnowledgePackSyncResult(bool Ok, string Message);

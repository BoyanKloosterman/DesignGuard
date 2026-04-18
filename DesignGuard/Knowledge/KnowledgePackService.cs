using System.IO;
using System.Text.Json;
using DesignGuard.Models;
using DesignGuard.Settings;

namespace DesignGuard.Knowledge;

/// <summary>Laadt knowledge packs van schijf; valideert grootte en structuur beperkt.</summary>
public sealed class KnowledgePackService
{
    public const long MaxPackFileBytes = 1_500_000;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _packsDirectory;
    private readonly UserSettingsService _userSettings;
    private IReadOnlyList<LoadedKnowledgePack> _packs = Array.Empty<LoadedKnowledgePack>();

    public KnowledgePackService(UserSettingsService userSettings)
    {
        _userSettings = userSettings;
        _packsDirectory = Path.Combine(AppContext.BaseDirectory, "KnowledgePacks");
    }

    /// <summary>Map waarin index.json en pack-JSON staan (naast executable).</summary>
    public string PacksDirectory => _packsDirectory;

    public IReadOnlyList<LoadedKnowledgePack> LoadedPacks => _packs;

    public void Reload()
    {
        _userSettings.Reload();
        var list = new List<LoadedKnowledgePack>();
        if (!Directory.Exists(_packsDirectory))
        {
            _packs = list;
            return;
        }

        var indexPath = Path.Combine(_packsDirectory, "index.json");
        if (!File.Exists(indexPath))
        {
            _packs = list;
            return;
        }

        KnowledgePackIndexDto? index;
        try
        {
            var ixJson = File.ReadAllText(indexPath);
            index = JsonSerializer.Deserialize<KnowledgePackIndexDto>(ixJson, JsonOpts);
        }
        catch
        {
            _packs = list;
            return;
        }

        if (index?.PackFiles == null) return;

        foreach (var rel in index.PackFiles)
        {
            if (rel.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
                continue;
            var full = Path.GetFullPath(Path.Combine(_packsDirectory, rel));
            if (!full.StartsWith(Path.GetFullPath(_packsDirectory), StringComparison.OrdinalIgnoreCase))
                continue;

            var fi = new FileInfo(full);
            if (!fi.Exists || fi.Length > MaxPackFileBytes)
                continue;

            try
            {
                var json = File.ReadAllText(full);
                var dto = JsonSerializer.Deserialize<KnowledgePackFileDto>(json, JsonOpts);
                if (dto == null || string.IsNullOrWhiteSpace(dto.PackId)) continue;
                if (!dto.IsActive || dto.IsArchived) continue;
                if (_userSettings.Current.DisabledPackIds.Contains(dto.PackId)) continue;
                list.Add(new LoadedKnowledgePack(dto, full));
            }
            catch
            {
                // Negeer corrupte pack (defensief).
            }
        }

        _packs = list;
    }

    /// <summary>Alle packs op schijf (actief volgens bestand), nog vóór gebruikers-toggle.</summary>
    public IReadOnlyList<LoadedKnowledgePack> DiscoverPacksIgnoringUserDisabled()
    {
        var list = new List<LoadedKnowledgePack>();
        if (!Directory.Exists(_packsDirectory)) return list;
        var indexPath = Path.Combine(_packsDirectory, "index.json");
        if (!File.Exists(indexPath)) return list;
        KnowledgePackIndexDto? index;
        try
        {
            index = JsonSerializer.Deserialize<KnowledgePackIndexDto>(File.ReadAllText(indexPath), JsonOpts);
        }
        catch
        {
            return list;
        }

        if (index?.PackFiles == null) return list;
        foreach (var rel in index.PackFiles)
        {
            if (rel.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(rel))
                continue;
            var full = Path.GetFullPath(Path.Combine(_packsDirectory, rel));
            if (!full.StartsWith(Path.GetFullPath(_packsDirectory), StringComparison.OrdinalIgnoreCase))
                continue;
            var fi = new FileInfo(full);
            if (!fi.Exists || fi.Length > MaxPackFileBytes)
                continue;
            try
            {
                var dto = JsonSerializer.Deserialize<KnowledgePackFileDto>(File.ReadAllText(full), JsonOpts);
                if (dto == null || string.IsNullOrWhiteSpace(dto.PackId)) continue;
                if (!dto.IsActive || dto.IsArchived) continue;
                list.Add(new LoadedKnowledgePack(dto, full));
            }
            catch
            {
                // genegeerd
            }
        }

        return list;
    }

    public void EnrichRequirement(RequirementModel r)
    {
        var ruleName = RuleNameFromFingerprint(r.RuleFingerprint);
        foreach (var pack in _packs)
        {
            foreach (var rule in pack.Dto.MappingRules)
            {
                if (string.IsNullOrWhiteSpace(rule.MatchRequirementRuleNameContains)) continue;
                if (!ruleName.Contains(rule.MatchRequirementRuleNameContains, StringComparison.OrdinalIgnoreCase))
                    continue;
                ApplyMapping(r.SourceAttribution, pack.Dto, rule.GuidanceItemIds);
                return;
            }
        }

        FallbackAttribution(r.SourceAttribution, r.SourceTags, true);
    }

    public void EnrichThreat(ThreatModel t)
    {
        var ruleName = RuleNameFromFingerprint(t.RuleFingerprint);
        foreach (var pack in _packs)
        {
            foreach (var rule in pack.Dto.MappingRules)
            {
                if (string.IsNullOrWhiteSpace(rule.MatchThreatRuleNameContains)) continue;
                if (!ruleName.Contains(rule.MatchThreatRuleNameContains, StringComparison.OrdinalIgnoreCase))
                    continue;
                ApplyMapping(t.SourceAttribution, pack.Dto, rule.GuidanceItemIds);
                return;
            }
        }

        FallbackAttribution(t.SourceAttribution, t.TriggerKeys, isRequirement: false);
    }

    private static void ApplyMapping(SourceAttributionModel target, KnowledgePackFileDto pack,
        List<string> itemIds)
    {
        target.KnowledgePackId = pack.PackId;
        target.KnowledgePackVersionLabel = pack.VersionLabel;
        target.KnowledgePackDisplayLabel = pack.DisplayLabel;
        target.SourceSummary = pack.SourceName;
        target.GuidanceItemIds = itemIds.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        var first = pack.Items.FirstOrDefault(i => target.GuidanceItemIds.Contains(i.Id));
        target.Nature = ParseNature(first?.GuidanceNature);
    }

    private static void FallbackAttribution(SourceAttributionModel target, IReadOnlyList<string> tags,
        bool isRequirement)
    {
        var nature = tags.Any(t => t.Contains("GDPR", StringComparison.OrdinalIgnoreCase) ||
                                   t.Contains("AVG", StringComparison.OrdinalIgnoreCase) ||
                                   t.Contains("NIS2", StringComparison.OrdinalIgnoreCase) ||
                                   t.Contains("CRA", StringComparison.OrdinalIgnoreCase))
            ? GuidanceNature.RegulationInspired
            : GuidanceNature.IndustryGuidanceInspired;

        target.KnowledgePackId = "designguard-fallback-v4";
        target.KnowledgePackVersionLabel = "v4";
        target.KnowledgePackDisplayLabel = "DesignGuard (fallback attributie)";
        target.Nature = nature;
        target.GuidanceItemIds = new List<string>();
        target.SourceSummary =
            isRequirement
                ? "Geen expliciete pack-mapping; tags en regels zijn richtinggevend."
                : "Geen expliciete pack-mapping; triggers en regels zijn richtinggevend.";
    }

    private static GuidanceNature ParseNature(string? s)
    {
        if (Enum.TryParse<GuidanceNature>(s, true, out var g))
            return g;
        return GuidanceNature.IndustryGuidanceInspired;
    }

    private static string RuleNameFromFingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint)) return "";
        var i = fingerprint.IndexOf(':');
        return i > 0 ? fingerprint[..i] : fingerprint;
    }

    public KnowledgeGuidanceItemDto? FindGuidanceItem(string packId, string itemId)
    {
        var pack = _packs.FirstOrDefault(p => p.Dto.PackId == packId);
        return pack?.Dto.Items.FirstOrDefault(i => i.Id == itemId);
    }

    public bool IsPackStale(LoadedKnowledgePack pack, int staleDays)
    {
        var refDate = pack.Dto.LastReviewedUtc;
        if (refDate == null &&
            DateTime.TryParse(pack.Dto.PublicationOrReviewDate, out var parsed))
            refDate = parsed.ToUniversalTime();
        if (refDate == null) return false;
        return (DateTime.UtcNow - refDate.Value).TotalDays > staleDays;
    }
}

public sealed record LoadedKnowledgePack(KnowledgePackFileDto Dto, string SourcePath);

using System.IO;
using System.Text.Json;
using DesignGuard.Models;

namespace DesignGuard.Services;

/// <summary>Laadt lokale control-bibliotheek (JSON) en koppelt aan dreigingen/eisen.</summary>
public sealed class ControlLibraryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private IReadOnlyList<ControlLibraryItemDto>? _cache;

    private IReadOnlyList<ControlLibraryItemDto> GetDefinitions()
    {
        if (_cache != null) return _cache;
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "control-library.json");
        if (!File.Exists(path))
        {
            _cache = Array.Empty<ControlLibraryItemDto>();
            return _cache;
        }

        var json = File.ReadAllText(path);
        var file = JsonSerializer.Deserialize<ControlLibraryFileDto>(json, JsonOpts);
        _cache = file?.Items ?? new List<ControlLibraryItemDto>();
        return _cache;
    }

    /// <summary>Voegt ontbrekende library-controls toe (idempotent op LibraryDefinitionId). Retourneert aantal toegevoegd.</summary>
    public int ApplyRecommendations(ProjectModel project)
    {
        var defs = GetDefinitions();
        if (defs.Count == 0)
            return -1;

        var added = 0;
        foreach (var def in defs)
        {
            if (string.IsNullOrWhiteSpace(def.Id)) continue;
            if (project.Controls.Any(c =>
                    string.Equals(c.LibraryDefinitionId, def.Id, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (!Matches(project, def)) continue;

            var linkedThreat = FindBestThreatStableId(project, def);
            var linkedReq = FindBestRequirementStableIds(project, def);

            project.Controls.Add(new ControlModel
            {
                StableId = Guid.NewGuid().ToString("N"),
                Title = def.Title,
                Category = def.Category,
                SourceTags = new List<string>(def.SourceTags),
                Description = def.Description,
                ImplementationGuidance = def.ImplementationGuidance,
                LinkedThreatStableId = linkedThreat ?? "",
                LinkedRequirementStableIds = linkedReq,
                Status = ControlLifecycleStatus.Proposed,
                LibraryDefinitionId = def.Id
            });
            added++;
        }

        return added;
    }

    private static string? FindBestThreatStableId(ProjectModel project, ControlLibraryItemDto def)
    {
        var needles = def.When?.AnyThreatTriggerContains ?? new List<string>();
        if (needles.Count == 0) return null;
        foreach (var t in project.Threats)
        {
            foreach (var key in t.TriggerKeys)
            {
                var lk = key.ToLowerInvariant();
                if (needles.Any(n => lk.Contains(n.ToLowerInvariant())))
                    return t.Id;
            }
        }

        return null;
    }

    private static List<string> FindBestRequirementStableIds(ProjectModel project, ControlLibraryItemDto def)
    {
        var needles = def.When?.AnyRequirementTriggerContains ?? new List<string>();
        var ids = new List<string>();
        if (needles.Count == 0) return ids;
        foreach (var r in project.Requirements)
        {
            foreach (var key in r.TriggerKeys)
            {
                var lk = key.ToLowerInvariant();
                if (!needles.Any(n => lk.Contains(n.ToLowerInvariant()))) continue;
                ids.Add(r.Id);
                break;
            }
        }

        return ids;
    }

    private static bool Matches(ProjectModel project, ControlLibraryItemDto def)
    {
        var w = def.When;
        if (w == null) return false;

        if (w.AnyProjectFlag.Count > 0 && w.AnyProjectFlag.Any(f => ProjectFlagTrue(project, f)))
            return true;

        if (w.AnyThreatTriggerContains.Count > 0 &&
            project.Threats.Any(t => t.TriggerKeys.Any(k =>
                w.AnyThreatTriggerContains.Any(n => k.Contains(n, StringComparison.OrdinalIgnoreCase)))))
            return true;

        if (w.AnyRequirementTriggerContains.Count > 0 &&
            project.Requirements.Any(r => r.TriggerKeys.Any(k =>
                w.AnyRequirementTriggerContains.Any(n => k.Contains(n, StringComparison.OrdinalIgnoreCase)))))
            return true;

        if (w.AnyComponentTagContains.Count > 0 &&
            project.Components.Any(c =>
                w.AnyComponentTagContains.Any(n =>
                    c.Tag.Contains(n, StringComparison.OrdinalIgnoreCase) ||
                    c.Name.Contains(n, StringComparison.OrdinalIgnoreCase))))
            return true;

        return false;
    }

    private static bool ProjectFlagTrue(ProjectModel p, string flag)
    {
        return flag switch
        {
            nameof(ProjectModel.HasAdmin) => p.HasAdmin,
            nameof(ProjectModel.PersonalDataProcessed) => p.PersonalDataProcessed,
            nameof(ProjectModel.InternetExposed) => p.InternetExposed,
            nameof(ProjectModel.FileUpload) => p.FileUpload,
            nameof(ProjectModel.ExternalApis) => p.ExternalApis,
            nameof(ProjectModel.SensitiveDataStored) => p.SensitiveDataStored,
            nameof(ProjectModel.LoggingMonitoringPresent) => p.LoggingMonitoringPresent,
            nameof(ProjectModel.HasAuthentication) => p.HasAuthentication,
            _ => false
        };
    }
}

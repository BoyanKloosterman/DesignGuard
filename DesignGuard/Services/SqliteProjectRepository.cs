using DesignGuard.Data;
using DesignGuard.Data.Entities;
using DesignGuard.Models;
using Microsoft.EntityFrameworkCore;

namespace DesignGuard.Services;

public sealed class SqliteProjectRepository : IProjectRepository
{
    private readonly IDbContextFactory<DesignGuardDbContext> _factory;

    public SqliteProjectRepository(IDbContextFactory<DesignGuardDbContext> factory)
    {
        _factory = factory;
    }

    public async Task EnsureDatabaseAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);
        await SqliteLegacySchema.EnsureAsync(db, ct);
    }

    public async Task<IReadOnlyList<(int Id, string Name, DateTime UpdatedAtUtc)>> ListSummariesAsync(
        CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Projects
            .AsNoTracking()
            .OrderByDescending(p => p.UpdatedAtUtc)
            .Select(p => new ValueTuple<int, string, DateTime>(p.Id, p.Name, p.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ProjectModel?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var e = await db.Projects
            .AsNoTracking()
            .Include(p => p.TrustBoundaries)
            .Include(p => p.Components).ThenInclude(c => c.TrustBoundary)
            .Include(p => p.DataFlows)
            .Include(p => p.UserRoles)
            .Include(p => p.Assets)
            .Include(p => p.DesignNotes)
            .Include(p => p.Controls)
            .Include(p => p.EntryPoints)
            .Include(p => p.SensitiveDataItems)
            .Include(p => p.ReviewItems)
            .Include(p => p.Snapshots)
            .Include(p => p.Threats)
            .Include(p => p.Requirements)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        return e == null ? null : ProjectMapper.ToModel(e);
    }

    public async Task<int> SaveAsync(ProjectModel model, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        if (model.Id == 0)
            return await InsertAsync(db, model, ct);

        var existing = await db.Projects
            .Include(p => p.TrustBoundaries)
            .Include(p => p.Components).ThenInclude(c => c.TrustBoundary)
            .Include(p => p.DataFlows)
            .Include(p => p.UserRoles)
            .Include(p => p.Assets)
            .Include(p => p.DesignNotes)
            .Include(p => p.Controls)
            .Include(p => p.EntryPoints)
            .Include(p => p.SensitiveDataItems)
            .Include(p => p.ReviewItems)
            .Include(p => p.Snapshots)
            .Include(p => p.Threats)
            .Include(p => p.Requirements)
            .FirstOrDefaultAsync(p => p.Id == model.Id, ct);
        if (existing == null)
            throw new InvalidOperationException($"Project {model.Id} niet gevonden.");

        ClearChildren(db, existing);
        await db.SaveChangesAsync(ct);

        ApplyProjectScalars(existing, model);
        AddTrustBoundaries(existing, model);
        await db.SaveChangesAsync(ct);

        var boundaryByName = existing.TrustBoundaries.ToDictionary(b => b.Name, b => b.Id, StringComparer.Ordinal);
        AddComponents(existing, model, boundaryByName);
        AddRoles(existing, model);
        await db.SaveChangesAsync(ct);

        var nameToCompId = existing.Components.ToDictionary(c => c.Name, c => c.Id, StringComparer.Ordinal);
        AddFlows(existing, model, nameToCompId);
        AddAssets(existing, model, nameToCompId);
        AddDesignNotes(existing, model);
        AddControls(existing, model);
        AddEntryPoints(existing, model, nameToCompId);
        AddSensitiveDataItems(existing, model, nameToCompId);
        AddReviewItems(existing, model);
        AddSnapshots(existing, model);
        await db.SaveChangesAsync(ct);

        var noteMap = BuildDesignNoteIdMap(model.DesignNotes, existing.DesignNotes);
        AddThreats(existing, model, noteMap);
        AddRequirements(existing, model, noteMap);
        await db.SaveChangesAsync(ct);

        await ReloadModelIdsAsync(db, model, existing.Id, ct);
        return existing.Id;
    }

    private static void ClearChildren(DesignGuardDbContext db, ProjectEntity existing)
    {
        db.Threats.RemoveRange(existing.Threats);
        db.Requirements.RemoveRange(existing.Requirements);
        db.Snapshots.RemoveRange(existing.Snapshots);
        db.ReviewItems.RemoveRange(existing.ReviewItems);
        db.SensitiveDataItems.RemoveRange(existing.SensitiveDataItems);
        db.EntryPoints.RemoveRange(existing.EntryPoints);
        db.Controls.RemoveRange(existing.Controls);
        db.DataFlows.RemoveRange(existing.DataFlows);
        foreach (var c in existing.Components)
            c.TrustBoundaryId = null;
        db.Components.RemoveRange(existing.Components);
        db.Assets.RemoveRange(existing.Assets);
        db.DesignNotes.RemoveRange(existing.DesignNotes);
        db.UserRoles.RemoveRange(existing.UserRoles);
        db.TrustBoundaries.RemoveRange(existing.TrustBoundaries);
        existing.Threats.Clear();
        existing.Requirements.Clear();
        existing.Snapshots.Clear();
        existing.ReviewItems.Clear();
        existing.SensitiveDataItems.Clear();
        existing.EntryPoints.Clear();
        existing.Controls.Clear();
        existing.DataFlows.Clear();
        existing.Components.Clear();
        existing.Assets.Clear();
        existing.DesignNotes.Clear();
        existing.UserRoles.Clear();
        existing.TrustBoundaries.Clear();
    }

    private static void ApplyProjectScalars(ProjectEntity e, ProjectModel m)
    {
        e.Name = m.Name;
        e.Description = m.Description;
        e.UpdatedAtUtc = DateTime.UtcNow;
        e.SystemName = m.SystemName;
        e.SystemType = m.SystemType.ToString();
        e.DeploymentContext = m.DeploymentContext.ToString();
        e.InternetExposed = m.InternetExposed;
        e.PersonalDataProcessed = m.PersonalDataProcessed;
        e.HasAuthentication = m.HasAuthentication;
        e.HasAdmin = m.HasAdmin;
        e.ExternalApis = m.ExternalApis;
        e.FileUpload = m.FileUpload;
        e.SensitiveDataStored = m.SensitiveDataStored;
        e.LoggingMonitoringPresent = m.LoggingMonitoringPresent;
        e.CriticalBusinessFunction = m.CriticalBusinessFunction;
        e.OpenIssuesSummary = m.OpenIssuesSummary;
        e.DismissedSuggestionKeysJson = JsonBlobs.Serialize(m.DismissedSuggestionKeys);
    }

    private static void AddTrustBoundaries(ProjectEntity e, ProjectModel m)
    {
        foreach (var tb in m.TrustBoundaries)
        {
            e.TrustBoundaries.Add(new TrustBoundaryEntity
            {
                Name = tb.Name,
                Description = tb.Description,
                Notes = tb.Notes,
                ColorHint = string.IsNullOrWhiteSpace(tb.ColorHint) ? "#4472C4" : tb.ColorHint
            });
        }
    }

    private static void AddComponents(ProjectEntity e, ProjectModel m,
        IReadOnlyDictionary<string, int> boundaryByName)
    {
        foreach (var c in m.Components)
        {
            int? tbId = null;
            if (c.TrustBoundaryId is { } oldTbId && oldTbId != 0)
            {
                var tbName = m.TrustBoundaries.FirstOrDefault(t => t.Id == oldTbId)?.Name;
                if (tbName != null && boundaryByName.TryGetValue(tbName, out var nid))
                    tbId = nid;
            }

            if (tbId == null && !string.IsNullOrWhiteSpace(c.TrustBoundaryName) &&
                boundaryByName.TryGetValue(c.TrustBoundaryName, out var byName))
                tbId = byName;

            e.Components.Add(new ComponentEntity
            {
                TrustBoundaryId = tbId,
                IsEntryPoint = c.IsEntryPoint,
                AssetClassification = c.AssetClassification.ToString(),
                DataSensitivity = c.StoresOrProcesses.ToString(),
                Notes = c.Notes,
                VisualX = c.VisualX,
                VisualY = c.VisualY,
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
            });
        }
    }

    private static void AddRoles(ProjectEntity e, ProjectModel m)
    {
        foreach (var r in m.UserRoles)
        {
            e.UserRoles.Add(new UserRoleEntity
            {
                Name = r.Name,
                Description = r.Description
            });
        }
    }

    private static void AddFlows(ProjectEntity e, ProjectModel m, Dictionary<string, int> nameToId)
    {
        foreach (var f in m.DataFlows)
        {
            ResolveFlowEndpoints(f, nameToId);
            if (f.FromComponentId == 0 || f.ToComponentId == 0) continue;
            e.DataFlows.Add(new DataFlowEntity
            {
                FromComponentId = f.FromComponentId,
                ToComponentId = f.ToComponentId,
                Label = f.Label,
                Notes = f.Notes
            });
        }
    }

    private static void AddAssets(ProjectEntity e, ProjectModel m, IReadOnlyDictionary<string, int> nameToCompId)
    {
        foreach (var a in m.Assets)
        {
            var related = 0;
            if (a.RelatedComponentId != 0)
            {
                var compName = m.Components.FirstOrDefault(c => c.Id == a.RelatedComponentId)?.Name;
                if (compName != null && nameToCompId.TryGetValue(compName, out var cid))
                    related = cid;
            }

            e.Assets.Add(new AssetEntity
            {
                Name = a.Name,
                Description = a.Description,
                Classification = a.Classification.ToString(),
                Sensitivity = a.Sensitivity.ToString(),
                Notes = a.Notes,
                RelatedComponentId = related
            });
        }
    }

    private static void AddDesignNotes(ProjectEntity e, ProjectModel m)
    {
        foreach (var n in m.DesignNotes)
        {
            e.DesignNotes.Add(new DesignNoteEntity
            {
                Kind = (int)n.Kind,
                Title = n.Title,
                Description = n.Description,
                Notes = n.Notes
            });
        }
    }

    private static void AddControls(ProjectEntity e, ProjectModel m)
    {
        foreach (var c in m.Controls)
        {
            e.Controls.Add(new ControlEntity
            {
                StableId = string.IsNullOrWhiteSpace(c.StableId) ? Guid.NewGuid().ToString("N") : c.StableId,
                Title = c.Title,
                Category = c.Category,
                SourceTagsJson = JsonBlobs.Serialize(c.SourceTags),
                Description = c.Description,
                ImplementationGuidance = c.ImplementationGuidance,
                LinkedThreatStableId = c.LinkedThreatStableId,
                LinkedRequirementStableIdsJson = JsonBlobs.Serialize(c.LinkedRequirementStableIds),
                Status = (int)c.Status,
                StatusNotes = c.StatusNotes,
                LibraryDefinitionId = c.LibraryDefinitionId
            });
        }
    }

    private static void AddEntryPoints(ProjectEntity e, ProjectModel m, IReadOnlyDictionary<string, int> nameToCompId)
    {
        foreach (var ep in m.EntryPoints)
        {
            var related = RemapComponentId(m, ep.RelatedComponentId, nameToCompId);
            e.EntryPoints.Add(new EntryPointEntity
            {
                Name = ep.Name,
                Description = ep.Description,
                RelatedComponentId = related,
                Notes = ep.Notes,
                ExposureNotes = ep.ExposureNotes
            });
        }
    }

    private static void AddSensitiveDataItems(ProjectEntity e, ProjectModel m,
        IReadOnlyDictionary<string, int> nameToCompId)
    {
        foreach (var s in m.SensitiveDataItems)
        {
            var related = RemapComponentId(m, s.RelatedComponentId, nameToCompId);
            e.SensitiveDataItems.Add(new SensitiveDataEntity
            {
                Name = s.Name,
                Category = s.Category,
                Description = s.Description,
                RelatedComponentId = related,
                StorageLocation = s.StorageLocation,
                Notes = s.Notes
            });
        }
    }

    private static int RemapComponentId(ProjectModel m, int oldId, IReadOnlyDictionary<string, int> nameToCompId)
    {
        if (oldId == 0) return 0;
        var compName = m.Components.FirstOrDefault(c => c.Id == oldId)?.Name;
        if (compName != null && nameToCompId.TryGetValue(compName, out var cid))
            return cid;
        return 0;
    }

    private static void AddReviewItems(ProjectEntity e, ProjectModel m)
    {
        foreach (var r in m.ReviewItems)
        {
            e.ReviewItems.Add(new ReviewItemEntity
            {
                SubjectKind = (int)r.SubjectKind,
                SubjectStableId = r.SubjectStableId,
                SubjectTitle = r.SubjectTitle,
                Status = (int)r.Status,
                Notes = r.Notes,
                Rationale = r.Rationale,
                Owner = r.Owner,
                CreatedAtUtc = r.CreatedAtUtc == default ? DateTime.UtcNow : r.CreatedAtUtc
            });
        }
    }

    private static void AddSnapshots(ProjectEntity e, ProjectModel m)
    {
        foreach (var s in m.Snapshots)
        {
            e.Snapshots.Add(new SnapshotEntity
            {
                Name = s.Name,
                CreatedAtUtc = s.CreatedAtUtc == default ? DateTime.UtcNow : s.CreatedAtUtc,
                SnapshotJson = s.SnapshotJson
            });
        }
    }

    private static Dictionary<int, int> BuildDesignNoteIdMap(
        IReadOnlyList<DesignNoteModel> oldNotes,
        List<DesignNoteEntity> newEntities)
    {
        var map = new Dictionary<int, int>();
        var used = new HashSet<int>();
        foreach (var old in oldNotes.Where(n => n.Id != 0))
        {
            var match = newEntities.FirstOrDefault(n =>
                !used.Contains(n.Id) &&
                n.Kind == (int)old.Kind &&
                n.Title == old.Title);
            if (match != null)
            {
                map[old.Id] = match.Id;
                used.Add(match.Id);
            }
        }

        return map;
    }

    private static void AddThreats(ProjectEntity e, ProjectModel m, IReadOnlyDictionary<int, int> noteMap)
    {
        foreach (var t in m.Threats)
        {
            var ent = ProjectMapper.ToThreatEntity(t, e.Id);
            ent.RelatedDesignNoteIdsJson = JsonBlobs.Serialize(
                RemapIds(t.RelatedDesignNoteIds, noteMap));
            e.Threats.Add(ent);
        }
    }

    private static void AddRequirements(ProjectEntity e, ProjectModel m, IReadOnlyDictionary<int, int> noteMap)
    {
        foreach (var r in m.Requirements)
        {
            var ent = ProjectMapper.ToRequirementEntity(r, e.Id);
            ent.RelatedDesignNoteIdsJson = JsonBlobs.Serialize(
                RemapIds(r.RelatedDesignNoteIds, noteMap));
            e.Requirements.Add(ent);
        }
    }

    private static List<int> RemapIds(IReadOnlyList<int> ids, IReadOnlyDictionary<int, int> map)
    {
        var list = new List<int>();
        foreach (var id in ids)
        {
            if (map.TryGetValue(id, out var n))
                list.Add(n);
        }

        return list;
    }

    private static void ResolveFlowEndpoints(DataFlowModel f, IReadOnlyDictionary<string, int> nameToId)
    {
        if (!string.IsNullOrWhiteSpace(f.SourceComponentName) &&
            nameToId.TryGetValue(f.SourceComponentName, out var fromId))
            f.FromComponentId = fromId;
        if (!string.IsNullOrWhiteSpace(f.TargetComponentName) &&
            nameToId.TryGetValue(f.TargetComponentName, out var toId))
            f.ToComponentId = toId;
    }

    private static async Task<int> InsertAsync(DesignGuardDbContext db, ProjectModel model, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var e = new ProjectEntity
        {
            Name = model.Name,
            Description = model.Description,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SystemName = model.SystemName,
            SystemType = model.SystemType.ToString(),
            DeploymentContext = model.DeploymentContext.ToString(),
            InternetExposed = model.InternetExposed,
            PersonalDataProcessed = model.PersonalDataProcessed,
            HasAuthentication = model.HasAuthentication,
            HasAdmin = model.HasAdmin,
            ExternalApis = model.ExternalApis,
            FileUpload = model.FileUpload,
            SensitiveDataStored = model.SensitiveDataStored,
            LoggingMonitoringPresent = model.LoggingMonitoringPresent,
            CriticalBusinessFunction = model.CriticalBusinessFunction,
            OpenIssuesSummary = model.OpenIssuesSummary,
            DismissedSuggestionKeysJson = JsonBlobs.Serialize(model.DismissedSuggestionKeys)
        };

        db.Projects.Add(e);
        await db.SaveChangesAsync(ct);

        AddTrustBoundaries(e, model);
        await db.SaveChangesAsync(ct);
        var boundaryByName = e.TrustBoundaries.ToDictionary(b => b.Name, b => b.Id, StringComparer.Ordinal);
        AddComponents(e, model, boundaryByName);
        AddRoles(e, model);
        await db.SaveChangesAsync(ct);
        var nameToId = e.Components.ToDictionary(c => c.Name, c => c.Id, StringComparer.Ordinal);
        AddFlows(e, model, nameToId);
        AddAssets(e, model, nameToId);
        AddDesignNotes(e, model);
        AddControls(e, model);
        AddEntryPoints(e, model, nameToId);
        AddSensitiveDataItems(e, model, nameToId);
        AddReviewItems(e, model);
        AddSnapshots(e, model);
        await db.SaveChangesAsync(ct);

        var noteMap = BuildDesignNoteIdMap(model.DesignNotes, e.DesignNotes);
        AddThreats(e, model, noteMap);
        AddRequirements(e, model, noteMap);
        await db.SaveChangesAsync(ct);

        model.Id = e.Id;
        await ReloadModelIdsAsync(db, model, e.Id, ct);
        return e.Id;
    }

    private static async Task ReloadModelIdsAsync(DesignGuardDbContext db, ProjectModel model, int projectId,
        CancellationToken ct)
    {
        var e = await db.Projects
            .AsNoTracking()
            .Include(p => p.TrustBoundaries)
            .Include(p => p.Components).ThenInclude(c => c.TrustBoundary)
            .Include(p => p.DataFlows)
            .Include(p => p.UserRoles)
            .Include(p => p.Assets)
            .Include(p => p.DesignNotes)
            .Include(p => p.Controls)
            .Include(p => p.EntryPoints)
            .Include(p => p.SensitiveDataItems)
            .Include(p => p.ReviewItems)
            .Include(p => p.Snapshots)
            .Include(p => p.Threats)
            .Include(p => p.Requirements)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (e == null) return;
        var fresh = ProjectMapper.ToModel(e);
        model.Id = fresh.Id;
        model.CreatedAtUtc = fresh.CreatedAtUtc;
        model.UpdatedAtUtc = fresh.UpdatedAtUtc;
        CopyLists(model, fresh);
    }

    private static void CopyLists(ProjectModel target, ProjectModel fresh)
    {
        target.TrustBoundaries.Clear();
        target.TrustBoundaries.AddRange(fresh.TrustBoundaries);
        target.Components.Clear();
        target.Components.AddRange(fresh.Components);
        target.DataFlows.Clear();
        target.DataFlows.AddRange(fresh.DataFlows);
        target.UserRoles.Clear();
        target.UserRoles.AddRange(fresh.UserRoles);
        target.Assets.Clear();
        target.Assets.AddRange(fresh.Assets);
        target.DesignNotes.Clear();
        target.DesignNotes.AddRange(fresh.DesignNotes);
        target.Controls.Clear();
        target.Controls.AddRange(fresh.Controls);
        target.EntryPoints.Clear();
        target.EntryPoints.AddRange(fresh.EntryPoints);
        target.SensitiveDataItems.Clear();
        target.SensitiveDataItems.AddRange(fresh.SensitiveDataItems);
        target.ReviewItems.Clear();
        target.ReviewItems.AddRange(fresh.ReviewItems);
        target.Snapshots.Clear();
        target.Snapshots.AddRange(fresh.Snapshots);
        target.DismissedSuggestionKeys.Clear();
        target.DismissedSuggestionKeys.AddRange(fresh.DismissedSuggestionKeys);
        target.Threats.Clear();
        target.Threats.AddRange(fresh.Threats);
        target.Requirements.Clear();
        target.Requirements.AddRange(fresh.Requirements);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var e = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (e != null)
        {
            db.Projects.Remove(e);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> EnsureDemoProjectAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        const string demoName = "Demo — Webshop (voorbeeld)";
        var existingId = await db.Projects.AsNoTracking()
            .Where(p => p.Name == demoName)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(ct);
        if (existingId != 0)
            return existingId;

        var demo = DemoProjectFactory.CreateDemoProject();
        demo.Name = demoName;
        return await InsertAsync(db, demo, ct);
    }
}

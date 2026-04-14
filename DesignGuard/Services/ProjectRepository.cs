using DesignGuard.Data;
using DesignGuard.Data.Entities;
using DesignGuard.Models;
using Microsoft.EntityFrameworkCore;

namespace DesignGuard.Services;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly IDbContextFactory<DesignGuardDbContext> _factory;

    public ProjectRepository(IDbContextFactory<DesignGuardDbContext> factory)
    {
        _factory = factory;
    }

    public async Task EnsureDatabaseAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);
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
            .Include(p => p.Components)
            .Include(p => p.DataFlows)
            .Include(p => p.UserRoles)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        return e == null ? null : ProjectMapper.ToModel(e);
    }

    public async Task<int> SaveAsync(ProjectModel model, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        if (model.Id == 0)
            return await InsertAsync(db, model, ct);

        var existing = await db.Projects
            .Include(p => p.Components)
            .Include(p => p.DataFlows)
            .Include(p => p.UserRoles)
            .FirstOrDefaultAsync(p => p.Id == model.Id, ct);
        if (existing == null)
            throw new InvalidOperationException($"Project {model.Id} niet gevonden.");

        db.Components.RemoveRange(existing.Components);
        db.DataFlows.RemoveRange(existing.DataFlows);
        db.UserRoles.RemoveRange(existing.UserRoles);
        await db.SaveChangesAsync(ct);

        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.UpdatedAtUtc = DateTime.UtcNow;
        existing.SystemName = model.SystemName;
        existing.SystemType = model.SystemType.ToString();
        existing.PersonalDataProcessed = model.PersonalDataProcessed;
        existing.HasAuthentication = model.HasAuthentication;
        existing.HasAdmin = model.HasAdmin;
        existing.ExternalApis = model.ExternalApis;
        existing.FileUpload = model.FileUpload;
        existing.SensitiveDataStored = model.SensitiveDataStored;

        foreach (var c in model.Components)
        {
            existing.Components.Add(new ComponentEntity
            {
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
            });
        }

        foreach (var r in model.UserRoles)
        {
            existing.UserRoles.Add(new UserRoleEntity
            {
                Name = r.Name,
                Description = r.Description
            });
        }

        await db.SaveChangesAsync(ct);

        var nameToId = existing.Components.ToDictionary(c => c.Name, c => c.Id);
        foreach (var f in model.DataFlows)
        {
            ResolveFlowEndpoints(f, nameToId);
            existing.DataFlows.Add(new DataFlowEntity
            {
                FromComponentId = f.FromComponentId,
                ToComponentId = f.ToComponentId,
                Label = f.Label,
                Notes = f.Notes
            });
        }

        await db.SaveChangesAsync(ct);
        await ReloadModelIdsAsync(db, model, existing.Id, ct);
        return existing.Id;
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
            PersonalDataProcessed = model.PersonalDataProcessed,
            HasAuthentication = model.HasAuthentication,
            HasAdmin = model.HasAdmin,
            ExternalApis = model.ExternalApis,
            FileUpload = model.FileUpload,
            SensitiveDataStored = model.SensitiveDataStored
        };

        foreach (var c in model.Components)
        {
            e.Components.Add(new ComponentEntity
            {
                Name = c.Name,
                Description = c.Description,
                Tag = c.Tag
            });
        }

        foreach (var r in model.UserRoles)
        {
            e.UserRoles.Add(new UserRoleEntity
            {
                Name = r.Name,
                Description = r.Description
            });
        }

        db.Projects.Add(e);
        await db.SaveChangesAsync(ct);

        var nameToId = e.Components.ToDictionary(c => c.Name, c => c.Id);
        foreach (var f in model.DataFlows)
        {
            ResolveFlowEndpoints(f, nameToId);
            e.DataFlows.Add(new DataFlowEntity
            {
                FromComponentId = f.FromComponentId,
                ToComponentId = f.ToComponentId,
                Label = f.Label,
                Notes = f.Notes
            });
        }

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
            .Include(p => p.Components)
            .Include(p => p.DataFlows)
            .Include(p => p.UserRoles)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (e == null) return;
        var fresh = ProjectMapper.ToModel(e);
        model.Id = fresh.Id;
        model.CreatedAtUtc = fresh.CreatedAtUtc;
        model.UpdatedAtUtc = fresh.UpdatedAtUtc;
        model.Components.Clear();
        model.Components.AddRange(fresh.Components);
        model.DataFlows.Clear();
        model.DataFlows.AddRange(fresh.DataFlows);
        model.UserRoles.Clear();
        model.UserRoles.AddRange(fresh.UserRoles);
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

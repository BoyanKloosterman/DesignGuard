using DesignGuard.Configuration;
using DesignGuard.Data;
using DesignGuard.Data.Mongo;
using DesignGuard.Data.Mongo.Documents;
using DesignGuard.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DesignGuard.Services;

public sealed class MongoProjectRepository : IProjectRepository
{
    private const string ProjectSeqKey = "projectSeq";

    private readonly IAppConfigurationService _config;
    private readonly MongoConnectionFactory _mongoFactory;

    public MongoProjectRepository(IAppConfigurationService config, MongoConnectionFactory mongoFactory)
    {
        _config = config;
        _mongoFactory = mongoFactory;
    }

    private IMongoCollection<ProjectDocument> Projects =>
        _mongoFactory.GetDatabase().GetCollection<ProjectDocument>(MongoCollectionNames.Projects);

    private IMongoCollection<BsonDocument> Meta =>
        _mongoFactory.GetDatabase().GetCollection<BsonDocument>(MongoCollectionNames.Meta);

    public Task EnsureDatabaseAsync(CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            return Task.CompletedTask;

        return EnsureDatabaseCoreAsync(ct);
    }

    private async Task EnsureDatabaseCoreAsync(CancellationToken ct)
    {
        var db = _mongoFactory.GetDatabase();
        await db.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: ct);
        var idx = new CreateIndexModel<ProjectDocument>(
            Builders<ProjectDocument>.IndexKeys.Descending(p => p.UpdatedAtUtc),
            new CreateIndexOptions { Name = "dg_projects_updatedAt_desc" });
        try
        {
            await Projects.Indexes.CreateOneAsync(idx, cancellationToken: ct);
        }
        catch (MongoCommandException)
        {
            // Index bestaat al — negeren.
        }
    }

    public async Task<IReadOnlyList<(int Id, string Name, DateTime UpdatedAtUtc)>> ListSummariesAsync(
        CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            return Array.Empty<(int, string, DateTime)>();

        var rows = await Projects.Find(FilterDefinition<ProjectDocument>.Empty)
            .SortByDescending(p => p.UpdatedAtUtc)
            .Project(p => new { p.Id, p.Name, p.UpdatedAtUtc })
            .ToListAsync(ct);
        return rows.Select(p => (p.Id, p.Name, p.UpdatedAtUtc)).ToList();
    }

    public async Task<ProjectModel?> GetAsync(int id, CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            return null;

        var doc = await Projects.Find(p => p.Id == id).FirstOrDefaultAsync(ct);
        return doc == null ? null : ProjectDocumentMapper.ToModel(doc);
    }

    public async Task<int> SaveAsync(ProjectModel model, CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            throw new InvalidOperationException("MongoDB niet geconfigureerd — kan niet opslaan.");

        if (model.Id == 0)
        {
            var newId = await NextProjectIdAsync(ct);
            var created = DateTime.UtcNow;
            var doc = ProjectDocumentBuilder.Build(model, newId, created);
            await Projects.InsertOneAsync(doc, cancellationToken: ct);
            await ReloadModelAsync(model, newId, ct);
            return newId;
        }

        var existing = await Projects.Find(p => p.Id == model.Id).FirstOrDefaultAsync(ct);
        var createdAt = existing?.CreatedAtUtc ?? model.CreatedAtUtc;
        if (createdAt == default)
            createdAt = DateTime.UtcNow;

        var updateDoc = ProjectDocumentBuilder.Build(model, model.Id, createdAt);
        await Projects.ReplaceOneAsync(p => p.Id == model.Id, updateDoc,
            new ReplaceOptions { IsUpsert = false }, ct);
        await ReloadModelAsync(model, model.Id, ct);
        return model.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            return;

        await Projects.DeleteOneAsync(p => p.Id == id, ct);
    }

    public async Task<int> EnsureDemoProjectAsync(CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            return 0;

        var demoName = DemoProjectFactory.DemoProjectDisplayName;
        var existingId = await Projects.Find(p => p.Name == demoName).Project(p => p.Id).FirstOrDefaultAsync(ct);
        if (existingId != 0)
            return existingId;

        var demo = DemoProjectFactory.CreateDemoProject();
        return await SaveAsync(demo, ct);
    }

    private async Task<int> NextProjectIdAsync(CancellationToken ct)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", ProjectSeqKey);
        var update = Builders<BsonDocument>.Update.Inc("seq", 1);
        var opts = new FindOneAndUpdateOptions<BsonDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };
        var doc = await Meta.FindOneAndUpdateAsync(filter, update, opts, ct);
        return doc["seq"].ToInt32();
    }

    private async Task ReloadModelAsync(ProjectModel model, int projectId, CancellationToken ct)
    {
        var doc = await Projects.Find(p => p.Id == projectId).FirstOrDefaultAsync(ct);
        if (doc == null) return;
        var fresh = ProjectDocumentMapper.ToModel(doc);
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
        target.C4Elements.Clear();
        target.C4Elements.AddRange(fresh.C4Elements);
        target.DismissedSuggestionKeys.Clear();
        target.DismissedSuggestionKeys.AddRange(fresh.DismissedSuggestionKeys);
        target.Threats.Clear();
        target.Threats.AddRange(fresh.Threats);
        target.Requirements.Clear();
        target.Requirements.AddRange(fresh.Requirements);
    }
}

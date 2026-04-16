using System.IO;
using DesignGuard.Configuration;
using DesignGuard.Data;
using DesignGuard.Data.Mongo;
using DesignGuard.Data.Mongo.Documents;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DesignGuard.Services;

public sealed class SqliteToMongoImportService
{
    private const string ProjectSeqKey = "projectSeq";

    private readonly IAppConfigurationService _config;
    private readonly MongoConnectionFactory _mongoFactory;

    public SqliteToMongoImportService(IAppConfigurationService config, MongoConnectionFactory mongoFactory)
    {
        _config = config;
        _mongoFactory = mongoFactory;
    }

    public async Task<SqliteImportResult> ImportAllProjectsAsync(string sqliteFilePath,
        IProgress<string>? progress = null, CancellationToken ct = default)
    {
        if (!_config.Current.IsMongoFullyConfigured)
            throw new InvalidOperationException("MongoDB is niet geconfigureerd; import niet mogelijk.");

        if (string.IsNullOrWhiteSpace(sqliteFilePath) || !File.Exists(sqliteFilePath))
            throw new FileNotFoundException("SQLite-bestand niet gevonden.", sqliteFilePath);

        var options = new DbContextOptionsBuilder<DesignGuardDbContext>()
            .UseSqlite($"Data Source={sqliteFilePath}")
            .Options;

        await using var db = new DesignGuardDbContext(options);
        if (!await db.Database.CanConnectAsync(ct))
            throw new InvalidOperationException("Kan geen verbinding maken met het SQLite-bestand.");

        await SqliteLegacySchema.EnsureAsync(db, ct);

        var ids = await db.Projects.AsNoTracking().Select(p => p.Id).ToListAsync(ct);
        var projectsColl = _mongoFactory.GetDatabase().GetCollection<ProjectDocument>(MongoCollectionNames.Projects);
        var meta = _mongoFactory.GetDatabase().GetCollection<BsonDocument>(MongoCollectionNames.Meta);

        var imported = 0;
        foreach (var id in ids)
        {
            ct.ThrowIfCancellationRequested();
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
            if (e == null) continue;

            var model = ProjectMapper.ToModel(e);
            var doc = ProjectDocumentBuilder.Build(model, id, model.CreatedAtUtc);
            await projectsColl.ReplaceOneAsync(p => p.Id == id, doc,
                new ReplaceOptions { IsUpsert = true }, cancellationToken: ct);
            imported++;
            progress?.Report($"Geïmporteerd: {model.Name} (id {id})");
        }

        var maxDoc = await projectsColl.Find(FilterDefinition<ProjectDocument>.Empty)
            .SortByDescending(p => p.Id)
            .Limit(1)
            .FirstOrDefaultAsync(ct);
        if (maxDoc != null)
        {
            await meta.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", ProjectSeqKey),
                Builders<BsonDocument>.Update.Max("seq", maxDoc.Id),
                new UpdateOptions { IsUpsert = true },
                cancellationToken: ct);
        }

        return new SqliteImportResult(imported, ids.Count);
    }
}

public sealed record SqliteImportResult(int ImportedCount, int SourceProjectCount);

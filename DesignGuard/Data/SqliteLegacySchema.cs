using Microsoft.EntityFrameworkCore;

namespace DesignGuard.Data;

/// <summary>SQLite: kolommen toevoegen voor oudere v3-bestanden (ook bij import).</summary>
public static class SqliteLegacySchema
{
    public static async Task EnsureAsync(DesignGuardDbContext db, CancellationToken ct = default)
    {
        var conn = db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        try
        {
            await AddColumnIfMissingAsync(conn, "Requirements", "SourceAttributionJson", "TEXT NOT NULL DEFAULT '{}'", ct);
            await AddColumnIfMissingAsync(conn, "Threats", "SourceAttributionJson", "TEXT NOT NULL DEFAULT '{}'", ct);
            await AddColumnIfMissingAsync(conn, "Controls", "LinkedComponentIdsJson", "TEXT NOT NULL DEFAULT '[]'", ct);
            await AddColumnIfMissingAsync(conn, "Projects", "GovernanceSecurityOwner", "TEXT NOT NULL DEFAULT ''", ct);
            await AddColumnIfMissingAsync(conn, "Projects", "GovernanceTechnicalOwner", "TEXT NOT NULL DEFAULT ''", ct);
            await AddColumnIfMissingAsync(conn, "Projects", "GovernanceComplianceStakeholder", "TEXT NOT NULL DEFAULT ''", ct);
            await AddColumnIfMissingAsync(conn, "Projects", "GovernanceReviewCadence", "TEXT NOT NULL DEFAULT ''", ct);
        }
        finally
        {
            await conn.CloseAsync();
        }
    }

    private static async Task AddColumnIfMissingAsync(
        System.Data.Common.DbConnection conn,
        string table,
        string column,
        string columnDef,
        CancellationToken ct)
    {
        await using var q = conn.CreateCommand();
        q.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{column}'";
        var n = Convert.ToInt64(await q.ExecuteScalarAsync(ct));
        if (n > 0) return;
        await using var alter = conn.CreateCommand();
        alter.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {columnDef}";
        await alter.ExecuteNonQueryAsync(ct);
    }
}

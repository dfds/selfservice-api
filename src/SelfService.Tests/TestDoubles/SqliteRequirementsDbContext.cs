using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;

namespace SelfService.Tests.TestDoubles;

/// <summary>
/// <see cref="RequirementsDbContext"/> on SQLite. The SQLite provider has no mapping for the
/// <see cref="System.Text.Json.Nodes.JsonObject"/> Labels column, so it is stored as TEXT.
/// </summary>
public sealed class SqliteRequirementsDbContext : RequirementsDbContext
{
    public SqliteRequirementsDbContext(DbContextOptions<RequirementsDbContext> options)
        : base(options) { }

    /// <summary>Creates the schema on <paramref name="connection"/>, which must already be open.</summary>
    public static async Task<SqliteRequirementsDbContext> Create(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<RequirementsDbContext>().UseSqlite(connection).Options;
        var db = new SqliteRequirementsDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder
            .Entity<RequirementsMetric>()
            .Property(metric => metric.Labels)
            .HasConversion(
                labels => labels == null ? null : labels.ToJsonString(null),
                json => json == null ? null : JsonNode.Parse(json, null, default)!.AsObject()
            )
            .HasColumnType("TEXT");
    }
}

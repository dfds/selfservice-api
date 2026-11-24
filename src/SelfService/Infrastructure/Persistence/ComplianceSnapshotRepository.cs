using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class ComplianceSnapshotRepository(SelfServiceDbContext dbContext) : IComplianceSnapshotRepository
{
    // The seed generator (db/seed/generate-compliance-history.py) writes the same format: camelCase
    // property names and camelCase enum strings.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task ReplaceForDate(
        DateOnly date,
        DateTime capturedAt,
        IReadOnlyCollection<CapabilityComplianceRow> rows,
        ComplianceDailySummaryResult summary
    )
    {
        // Joins the caller's transaction when there is one.
        await using var transaction = await new RealDbTransactionFacade(dbContext).BeginTransaction();

        DetachTracked(d => d == date);
        await dbContext.ComplianceSnapshots.Where(x => x.SnapshotDate == date).ExecuteDeleteAsync();
        await dbContext.ComplianceDailySummaries.Where(x => x.SnapshotDate == date).ExecuteDeleteAsync();

        await dbContext.ComplianceSnapshots.AddRangeAsync(
            rows.Select(row => new ComplianceSnapshot(
                id: Guid.NewGuid(),
                snapshotDate: date,
                capturedAt: capturedAt,
                capabilityId: row.CapabilityId,
                capabilityName: row.CapabilityName,
                jsonMetadata: row.JsonMetadata,
                costCentre: row.CostCentre,
                memberCount: row.MemberCount,
                overallStatus: row.OverallStatus,
                categories: JsonSerializer.Serialize(row.Categories, JsonOptions)
            ))
        );

        await dbContext.ComplianceDailySummaries.AddAsync(
            new ComplianceDailySummary(
                snapshotDate: date,
                capturedAt: capturedAt,
                totalCapabilities: summary.TotalCapabilities,
                fullyCompliantCapabilities: summary.FullyCompliantCapabilities,
                nonCompliantCapabilities: summary.NonCompliantCapabilities,
                unknownCapabilities: summary.UnknownCapabilities,
                requirements: JsonSerializer.Serialize(summary.Requirements, JsonOptions),
                costCentres: JsonSerializer.Serialize(summary.CostCentres, JsonOptions)
            )
        );

        await transaction.Commit();
    }

    public async Task<List<CapabilityComplianceRow>> GetForDate(DateOnly date)
    {
        var snapshots = await dbContext
            .ComplianceSnapshots.AsNoTracking()
            .Where(x => x.SnapshotDate == date)
            .OrderBy(x => x.CapabilityId)
            .ToListAsync();

        return snapshots.Select(ToRow).ToList();
    }

    public async Task<List<ComplianceDailySummaryResult>> GetSummaries(DateOnly from, DateOnly to)
    {
        var summaries = await dbContext
            .ComplianceDailySummaries.AsNoTracking()
            .Where(x => x.SnapshotDate >= from && x.SnapshotDate <= to)
            .OrderBy(x => x.SnapshotDate)
            .ToListAsync();

        return summaries
            .Select(x => new ComplianceDailySummaryResult
            {
                Date = x.SnapshotDate,
                CapturedAt = x.CapturedAt,
                TotalCapabilities = x.TotalCapabilities,
                FullyCompliantCapabilities = x.FullyCompliantCapabilities,
                NonCompliantCapabilities = x.NonCompliantCapabilities,
                UnknownCapabilities = x.UnknownCapabilities,
                Requirements = Deserialize<List<ComplianceRequirementTotals>>(x.Requirements),
                CostCentres = Deserialize<List<ComplianceCostCentreTotals>>(x.CostCentres),
            })
            .ToList();
    }

    public async Task<List<CapabilityComplianceHistoryEntry>> GetCapabilityHistory(
        string capabilityId,
        DateOnly from,
        DateOnly to
    )
    {
        var snapshots = await dbContext
            .ComplianceSnapshots.AsNoTracking()
            .Where(x => x.CapabilityId == capabilityId && x.SnapshotDate >= from && x.SnapshotDate <= to)
            .OrderBy(x => x.SnapshotDate)
            .ToListAsync();

        return snapshots.Select(x => new CapabilityComplianceHistoryEntry(x.SnapshotDate, ToRow(x))).ToList();
    }

    public Task<DateOnly?> GetOldestDate() =>
        dbContext.ComplianceDailySummaries.MinAsync(x => (DateOnly?)x.SnapshotDate);

    public async Task DeleteOlderThan(DateOnly cutoff)
    {
        await using var transaction = await new RealDbTransactionFacade(dbContext).BeginTransaction();

        DetachTracked(d => d < cutoff);
        await dbContext.ComplianceSnapshots.Where(x => x.SnapshotDate < cutoff).ExecuteDeleteAsync();
        await dbContext.ComplianceDailySummaries.Where(x => x.SnapshotDate < cutoff).ExecuteDeleteAsync();

        await transaction.Commit();
    }

    // ExecuteDeleteAsync bypasses the change tracker. Entities added by an earlier ReplaceForDate on this
    // context stay tracked, and re-adding a summary for the same date would conflict on its key.
    private void DetachTracked(Func<DateOnly, bool> isDeleted)
    {
        var snapshots = dbContext
            .ChangeTracker.Entries<ComplianceSnapshot>()
            .Where(e => isDeleted(e.Entity.SnapshotDate));
        var summaries = dbContext
            .ChangeTracker.Entries<ComplianceDailySummary>()
            .Where(e => isDeleted(e.Entity.SnapshotDate));

        foreach (var entry in snapshots.Cast<EntityEntry>().Concat(summaries).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static CapabilityComplianceRow ToRow(ComplianceSnapshot snapshot) =>
        new(
            CapabilityId: snapshot.CapabilityId,
            CapabilityName: snapshot.CapabilityName,
            JsonMetadata: snapshot.JsonMetadata,
            CostCentre: snapshot.CostCentre,
            MemberCount: snapshot.MemberCount,
            OverallStatus: snapshot.OverallStatus,
            Categories: Deserialize<List<ComplianceCategoryResult>>(snapshot.Categories)
        );

    private static T Deserialize<T>(string json)
        where T : new() => JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();
}

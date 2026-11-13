using SelfService.Application;

namespace SelfService.Domain.Models;

public interface IComplianceSnapshotRepository
{
    /// <summary>
    /// Replaces every row for <paramref name="date"/> and its summary in one transaction. Calling it again
    /// for the same date leaves one set of rows.
    /// </summary>
    Task ReplaceForDate(
        DateOnly date,
        DateTime capturedAt,
        IReadOnlyCollection<CapabilityComplianceRow> rows,
        ComplianceDailySummaryResult summary
    );

    /// <summary>Empty when nothing was captured on <paramref name="date"/>.</summary>
    Task<List<CapabilityComplianceRow>> GetForDate(DateOnly date);

    /// <summary>Summaries with <paramref name="from"/> ≤ date ≤ <paramref name="to"/>, oldest first.</summary>
    Task<List<ComplianceDailySummaryResult>> GetSummaries(DateOnly from, DateOnly to);

    /// <summary>Entries with <paramref name="from"/> ≤ date ≤ <paramref name="to"/>, oldest first.</summary>
    Task<List<CapabilityComplianceHistoryEntry>> GetCapabilityHistory(string capabilityId, DateOnly from, DateOnly to);

    Task<DateOnly?> GetOldestDate();

    /// <summary>Deletes rows and summaries dated strictly before <paramref name="cutoff"/>.</summary>
    Task DeleteOlderThan(DateOnly cutoff);
}

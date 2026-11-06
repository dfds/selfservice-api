namespace SelfService.Domain.Models;

/// <summary>
/// Platform-wide compliance totals for one UTC day, written together with that day's
/// <see cref="ComplianceSnapshot"/> rows. <see cref="Requirements"/> and <see cref="CostCentres"/> hold
/// serialised JSON; <c>ComplianceSnapshotRepository</c> owns the format.
/// </summary>
public class ComplianceDailySummary
{
    public ComplianceDailySummary(
        DateOnly snapshotDate,
        DateTime capturedAt,
        int totalCapabilities,
        int fullyCompliantCapabilities,
        int nonCompliantCapabilities,
        int unknownCapabilities,
        string requirements,
        string costCentres
    )
    {
        SnapshotDate = snapshotDate;
        CapturedAt = capturedAt;
        TotalCapabilities = totalCapabilities;
        FullyCompliantCapabilities = fullyCompliantCapabilities;
        NonCompliantCapabilities = nonCompliantCapabilities;
        UnknownCapabilities = unknownCapabilities;
        Requirements = requirements;
        CostCentres = costCentres;
    }

    public DateOnly SnapshotDate { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public int TotalCapabilities { get; private set; }
    public int FullyCompliantCapabilities { get; private set; }
    public int NonCompliantCapabilities { get; private set; }
    public int UnknownCapabilities { get; private set; }
    public string Requirements { get; private set; }
    public string CostCentres { get; private set; }
}

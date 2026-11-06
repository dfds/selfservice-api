using SelfService.Application;

namespace SelfService.Domain.Models;

/// <summary>
/// One capability's computed compliance on one UTC day. <see cref="Categories"/> holds the serialised
/// <c>List&lt;ComplianceCategoryResult&gt;</c>; <c>ComplianceSnapshotRepository</c> owns the format.
/// </summary>
public class ComplianceSnapshot : Entity<Guid>
{
    public ComplianceSnapshot(
        Guid id,
        DateOnly snapshotDate,
        DateTime capturedAt,
        string capabilityId,
        string capabilityName,
        string? jsonMetadata,
        string? costCentre,
        int memberCount,
        ComplianceStatus overallStatus,
        string categories
    )
        : base(id)
    {
        SnapshotDate = snapshotDate;
        CapturedAt = capturedAt;
        CapabilityId = capabilityId;
        CapabilityName = capabilityName;
        JsonMetadata = jsonMetadata;
        CostCentre = costCentre;
        MemberCount = memberCount;
        OverallStatus = overallStatus;
        Categories = categories;
    }

    public DateOnly SnapshotDate { get; private set; }
    public DateTime CapturedAt { get; private set; }
    public string CapabilityId { get; private set; }
    public string CapabilityName { get; private set; }
    public string? JsonMetadata { get; private set; }

    /// <summary>Null for a rogue capability.</summary>
    public string? CostCentre { get; private set; }

    /// <summary>0 for an orphaned capability.</summary>
    public int MemberCount { get; private set; }
    public ComplianceStatus OverallStatus { get; private set; }
    public string Categories { get; private set; }
}

using SelfService.Domain.Models;

namespace SelfService.Application;

/// <summary>
/// A null <c>date</c> computes live; any other value reads that UTC day's snapshot and throws
/// <see cref="Domain.Exceptions.ComplianceSnapshotNotFoundException"/> when the day has none.
/// </summary>
public interface IComplianceApplicationService
{
    Task<ComplianceSummaryResult> GetComplianceSummary(DateOnly? date = null);
    Task<CapabilityComplianceResult> GetCapabilityCompliance(CapabilityId capabilityId, DateOnly? date = null);
    Task<CostCentreComplianceResult> GetCostCentreCompliance(string costCentre, DateOnly? date = null);
    Task<CostCentreComplianceDetailsResult> GetCostCentreComplianceDetails(string costCentre, DateOnly? date = null);
    Task<AllCostCentresComplianceResult> GetAllCostCentresCompliance(DateOnly? date = null);
    Task<CostCentreComplianceResult> GetRogueCapabilitiesCompliance(DateOnly? date = null);
    Task<CostCentreComplianceDetailsResult> GetRogueCapabilitiesComplianceDetails(DateOnly? date = null);
    Task<CostCentreComplianceResult> GetOrphanedCapabilitiesCompliance(DateOnly? date = null);
    Task<CostCentreComplianceDetailsResult> GetOrphanedCapabilitiesComplianceDetails(DateOnly? date = null);
    Task<RequirementsComplianceResult> GetRequirementsCompliance(DateOnly? date = null);
    Task<RequirementComplianceDetailsResult> GetRequirementComplianceDetails(
        string requirementId,
        DateOnly? date = null
    );

    /// <summary>Computes every active capability live, dated by the current UTC day.</summary>
    Task<ComplianceCaptureResult> CaptureCurrent();

    /// <summary>
    /// Daily summaries, oldest first: stored ones, and today (UTC) computed live. <c>from</c> defaults to the oldest
    /// snapshot, <c>to</c> to today.
    /// </summary>
    Task<ComplianceHistoryResult> GetHistory(DateOnly? from = null, DateOnly? to = null);

    /// <summary>
    /// <see cref="GetHistory"/> narrowed to one cost centre, rogue or orphaned group, with its per-requirement
    /// totals. <c>costCentre</c> is matched case-insensitively and ignored for the rogue and orphaned groups.
    /// </summary>
    Task<CostCentreComplianceHistoryResult> GetCostCentreHistory(
        ComplianceCostCentreKind kind,
        string? costCentre,
        DateOnly? from = null,
        DateOnly? to = null
    );

    /// <summary>
    /// <see cref="GetHistory"/> narrowed to one requirement, with its totals per cost centre. Throws
    /// <see cref="KeyNotFoundException"/> for an unknown requirement id.
    /// </summary>
    Task<RequirementComplianceHistoryResult> GetRequirementHistory(
        string requirementId,
        DateOnly? from = null,
        DateOnly? to = null
    );

    /// <summary>
    /// One capability's days with <c>from</c> ≤ date ≤ <c>to</c>, oldest first: stored snapshots, and today (UTC)
    /// computed live while the capability is active.
    /// </summary>
    Task<List<CapabilityComplianceHistoryEntry>> GetCapabilityHistory(
        CapabilityId capabilityId,
        DateOnly from,
        DateOnly to
    );
}

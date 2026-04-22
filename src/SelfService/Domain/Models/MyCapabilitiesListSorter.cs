namespace SelfService.Domain.Models;

public static class CapabilityPriorityScore
{

    private const double PendingDeletionScore = 100.0;
    private const double PendingMembershipApplicationScore = 1.0;
    private const double ComplianceWeight = 0.5;
    private const double OwnerWeight = 10.0;

    // -----------------------------------------------------------------------

    public static double Compute(
        Capability capability,
        CapabilityOutstandingActions actions,
        IReadOnlySet<CapabilityId> ownedCapabilityIds
    ) =>
        (actions.IsPendingDeletion ? PendingDeletionScore : 0.0)
        + (actions.PendingMembershipApplicationCount * PendingMembershipApplicationScore)
        + (100.0 - (capability.RequirementScore ?? 100.0)) * ComplianceWeight
        + (ownedCapabilityIds.Contains(capability.Id) ? OwnerWeight : 0.0);
}


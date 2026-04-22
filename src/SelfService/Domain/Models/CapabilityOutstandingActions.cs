namespace SelfService.Domain.Models;

public class CapabilityOutstandingActions
{
    public bool IsPendingDeletion { get; init; }

    public int PendingMembershipApplicationCount { get; init; }

    public static readonly CapabilityOutstandingActions None = new();
}

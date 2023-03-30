namespace SelfService.Domain.Models;

[Obsolete("Turn this into a value object instead (see MessageContractStatus)")]
public enum MembershipApplicationStatusOptions
{
    Unknown = 0,
    PendingApprovals,
    Finalized,
    Cancelled
}
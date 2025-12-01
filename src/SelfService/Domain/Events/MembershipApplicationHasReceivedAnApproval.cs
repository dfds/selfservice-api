using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MembershipApplicationHasReceivedAnApproval : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? ApprovedFor { get; set; }
    public string? CapabilityId { get; set; }
    public string[]? CapabilityApprovers { get; set; }
}

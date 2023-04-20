using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MembershipApplicationHasReceivedAnApproval : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
}
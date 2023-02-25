using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MembershipApplicationHasRecievedAnApproval : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
}
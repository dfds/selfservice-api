using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MembershipApplicationHasBeenFinalized : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
}
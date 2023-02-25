using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class MembershipApplicationHasBeenCancelled : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
}
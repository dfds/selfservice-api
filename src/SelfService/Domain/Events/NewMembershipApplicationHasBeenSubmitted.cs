using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMembershipApplicationHasBeenSubmitted : IDomainEvent
{
    public string? MembershipApplicationId { get; set; }
}

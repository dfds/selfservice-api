using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMembershipInvitationHasBeenAccepted : IDomainEvent
{
    public string? MembershipInvitationId { get; set; }
}

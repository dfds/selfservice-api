using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMembershipInvitationHasBeenCancelled : IDomainEvent
{
    public string? MembershipInvitationId { get; set; }
}

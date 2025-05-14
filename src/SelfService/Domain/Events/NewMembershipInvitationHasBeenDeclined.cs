using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMembershipInvitationHasBeenDeclined : IDomainEvent
{
    public string? MembershipInvitationId { get; set; }
}

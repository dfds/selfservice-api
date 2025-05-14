using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMembershipInvitationHasBeenSubmitted : IDomainEvent
{
    public string? MembershipInvitationId { get; set; }
    public string? Invitee { get; set; }
    public string? TargetId { get; set; }
    public string? TargetType { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IInvitationApplicationService
{
    Task<List<Invitation>> GetActiveInvitations(UserId userId);
    Task<Invitation> CreateInvitation(UserId invitee, Guid target, UserId createdBy);
    Task<Invitation> AcceptInvitation(InvitationId invitationId);
    Task<Invitation> DeclineInvitation(InvitationId invitationId);
}

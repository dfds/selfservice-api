using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IInvitationApplicationService
{
    Task<List<Invitation>> GetActiveInvitations(UserId userId);
    Task<List<Invitation>> GetActiveInvitationsForType(UserId userId, InvitationTargetTypeOptions targetType);
    Task<Invitation> GetInvitation(InvitationId invitationId);
    Task<Invitation> CreateInvitation(
        UserId invitee,
        string description,
        Guid targetId,
        InvitationTargetTypeOptions targetType,
        UserId createdBy
    );
    Task<Invitation> AcceptInvitation(InvitationId invitationId);
    Task<Invitation> DeclineInvitation(InvitationId invitationId);
}

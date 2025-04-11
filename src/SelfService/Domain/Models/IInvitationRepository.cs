namespace SelfService.Domain.Models;

public interface IInvitationRepository : IGenericRepository<Invitation, InvitationId>
{
    Task<List<Invitation>> GetExpiredInvitations();
    Task<List<Invitation>> GetActiveInvitations(UserId userId, string targetId);
    Task<List<Invitation>> GetOtherActiveInvitationsForSameTarget(
        UserId userId,
        string targetId,
        InvitationId invitationId
    );
}

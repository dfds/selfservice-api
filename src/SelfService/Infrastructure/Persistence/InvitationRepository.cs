using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class InvitationRepository : GenericRepository<Invitation, InvitationId>, IInvitationRepository
{
    public InvitationRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Invitations) { }

    public async Task<List<Invitation>> GetActiveInvitations(UserId userId, string targetId)
    {
        var invitations = await GetAllWithPredicate(x =>
            x.Invitee == userId && x.Status == InvitationStatusOptions.Active && x.TargetId == targetId
        );
        return invitations;
    }

    public async Task<List<Invitation>> GetOtherActiveInvitationsForSameTarget(
        UserId userId,
        string targetId,
        InvitationId invitationId
    )
    {
        var invitations = await GetAllWithPredicate(x =>
            x.Invitee == userId
            && x.Status == InvitationStatusOptions.Active
            && x.TargetId == targetId
            && x.Id != invitationId
        );
        return invitations;
    }
}

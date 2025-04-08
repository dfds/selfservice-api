using SelfService.Domain;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class InvitationRepository : GenericRepository<Invitation, InvitationId>, IInvitationRepository
{
    private readonly SystemTime _systemTime;

    public InvitationRepository(SelfServiceDbContext dbContext, SystemTime systemTime)
        : base(dbContext.Invitations)
    {
        _systemTime = systemTime;
    }

    public async Task<List<Invitation>> GetActiveInvitations(UserId userId, string targetId)
    {
        var invitations = await GetAllWithPredicate(x =>
            x.Invitee == userId && x.Status == InvitationStatusOptions.Active && x.TargetId == targetId
        );
        return invitations;
    }

    public async Task<List<Invitation>> GetExpiredInvitations()
    {
        var now = _systemTime.Now;
        var expiredInvitations = await GetAllWithPredicate(x => x.CreatedAt < now);
        return expiredInvitations;
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

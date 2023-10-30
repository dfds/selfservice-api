using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class InvitationRepository : GenericRepository<Invitation, InvitationId>, IInvitationRepository
{
    public InvitationRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Invitations) { }
}

using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class TeamCapabilityLinkingRepository
    : GenericRepository<TeamCapabilityLink, Guid>,
        ITeamCapabilityLinkingRepository
{
    public TeamCapabilityLinkingRepository(SelfServiceDbContext dbContext)
        : base(dbContext.TeamCapabilityLinks) { }

    public Task<TeamCapabilityLink?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId)
    {
        return DbSetReference.FirstOrDefaultAsync(x => x.TeamId == teamId && x.CapabilityId == capabilityId);
    }
}

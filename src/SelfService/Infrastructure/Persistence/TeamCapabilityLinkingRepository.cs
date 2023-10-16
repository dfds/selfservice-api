using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

//TODO: Replace with generic repository
public class TeamCapabilityLinkingRepository
    : GenericRepository<TeamCapabilityLink, Guid>,
        ITeamCapabilityLinkingRepository
{
    public TeamCapabilityLinkingRepository(SelfServiceDbContext dbContext)
        : base(dbContext.TeamCapabilityLinks) { }

    public Task<TeamCapabilityLink?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId)
    {
        throw new NotImplementedException();
    }
}

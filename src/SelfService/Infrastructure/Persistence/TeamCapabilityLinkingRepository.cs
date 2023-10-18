using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class TeamCapabilityLinkingRepository
    : GenericRepository<TeamCapabilityLink, TeamCapabilityLinkId>,
        ITeamCapabilityLinkingRepository
{
    public TeamCapabilityLinkingRepository(SelfServiceDbContext dbContext)
        : base(dbContext.TeamCapabilityLinks) { }
}

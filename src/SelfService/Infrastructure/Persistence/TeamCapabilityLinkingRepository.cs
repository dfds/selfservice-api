using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

//TODO: Replace with generic repository
public class TeamCapabilityLinkingRepository : ITeamCapabilityLinkingRepository
{
    public Task Add(TeamCapabilityLink link)
    {
        throw new NotImplementedException();
    }

    public Task Remove(TeamCapabilityLink link)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<TeamCapabilityLink>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<TeamCapabilityLink?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId)
    {
        throw new NotImplementedException();
    }
}

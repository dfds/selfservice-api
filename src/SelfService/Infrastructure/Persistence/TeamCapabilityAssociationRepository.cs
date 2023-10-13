using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

//TODO: Replace with generic repository
public class TeamCapabilityAssociationRepository : ITeamCapabilityAssociationRepository
{
    public Task Add(TeamCapabilityAssociation association)
    {
        throw new NotImplementedException();
    }

    public Task Remove(TeamCapabilityAssociation association)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<TeamCapabilityAssociation>> GetAll()
    {
        throw new NotImplementedException();
    }

    public Task<TeamCapabilityAssociation?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId)
    {
        throw new NotImplementedException();
    }
}

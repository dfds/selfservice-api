namespace SelfService.Domain.Models;

public interface ITeamCapabilityAssociationRepository
{
    Task Add(TeamCapabilityAssociation association);
    Task Remove(TeamCapabilityAssociation association);
    Task<IEnumerable<TeamCapabilityAssociation>> GetAll();
    Task<TeamCapabilityAssociation?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId);
}

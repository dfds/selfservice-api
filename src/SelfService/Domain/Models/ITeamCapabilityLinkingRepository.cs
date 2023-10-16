namespace SelfService.Domain.Models;

public interface ITeamCapabilityLinkingRepository
{
    Task Add(TeamCapabilityLink link);
    Task Remove(TeamCapabilityLink link);
    Task<IEnumerable<TeamCapabilityLink>> GetAll();
    Task<TeamCapabilityLink?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId);
}

namespace SelfService.Domain.Models;

public interface ITeamCapabilityLinkingRepository : IGenericRepository<TeamCapabilityLink, Guid>
{
    Task<TeamCapabilityLink?> FindByTeamAndCapabilityIds(TeamId teamId, CapabilityId capabilityId);
}

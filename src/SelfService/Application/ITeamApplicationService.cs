using SelfService.Domain.Models;

namespace SelfService.Application;

public interface ITeamApplicationService
{
    Task<List<Team>> GetAllTeams();
    Task<Team?> GetTeam(TeamId id);
    Task<Team> AddTeam(string name, string description, UserId createdBy, List<CapabilityId> linkedCapabilityIds);
    Task RemoveTeam(TeamId id);
    Task<TeamCapabilityLink> AddLinkToCapability(TeamId teamId, CapabilityId capabilityId);
    Task RemoveLinkToCapability(TeamId teamId, CapabilityId capabilityId);
}

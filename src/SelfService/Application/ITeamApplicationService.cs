using SelfService.Domain.Models;

namespace SelfService.Application;

public interface ITeamApplicationService
{
    Task<IEnumerable<Team>> GetAllTeams();
    Task<Team?> GetTeam(TeamId id);
    Task<Team> AddTeam(string name, string description, UserId createdBy);
    Task RemoveTeam(TeamId id);
    Task AddAssociationWithCapability(TeamId teamId, CapabilityId capabilityId);
    Task RemoveAssociationWithCapability(TeamId teamId, CapabilityId capabilityId);
}

using SelfService.Domain.Models;

namespace SelfService.Application;

public class TeamApplicationService : ITeamApplicationService
{
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamCapabilityLinkingRepository _teamCapabilityLinkingRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly ILogger<TeamApplicationService> _logger;

    public TeamApplicationService(
        ITeamRepository teamRepository,
        ITeamCapabilityLinkingRepository teamCapabilityLinkingRepository,
        ICapabilityRepository capabilityRepository,
        ILogger<TeamApplicationService> logger
    )
    {
        _teamRepository = teamRepository;
        _teamCapabilityLinkingRepository = teamCapabilityLinkingRepository;
        _capabilityRepository = capabilityRepository;
        _logger = logger;
    }

    public Task<List<Team>> GetAllTeams()
    {
        return _teamRepository.GetAll();
    }

    public Task<Team?> GetTeam(TeamId id)
    {
        return _teamRepository.FindBy(id);
    }

    public async Task<Team> AddTeam(string name, string description, UserId createdBy)
    {
        var teamWithThisName = await _teamRepository.FindByName(name);
        if (teamWithThisName != null)
        {
            throw new ArgumentException(
                $"Team with name {name} already exists, please use the other team or choose a different name."
            );
        }

        var newTeam = new Team(TeamId.New(), name, description, createdBy, DateTime.UtcNow);
        await _teamRepository.Add(newTeam);
        return newTeam;
    }

    public Task RemoveTeam(TeamId id)
    {
        _teamRepository.Remove(id);
        return Task.CompletedTask;
    }

    public async Task AddLinkToCapability(TeamId teamId, CapabilityId capabilityId)
    {
        var team = await _teamRepository.FindBy(teamId);
        if (team == null)
        {
            throw new ArgumentException("Team does not exist");
        }

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
        {
            throw new ArgumentException("Capability does not exist");
        }

        var linking = await _teamCapabilityLinkingRepository.FindByTeamAndCapabilityIds(teamId, capabilityId);

        if (linking != null)
        {
            _logger.LogWarning(
                "Attempted to add a link between team {teamId} and capability {capabilityId}, but such linking already exists.",
                teamId,
                capabilityId
            );
            return;
        }

        await _teamCapabilityLinkingRepository.Add(new TeamCapabilityLink(teamId, capabilityId));
    }

    public async Task RemoveLinkToCapability(TeamId teamId, CapabilityId capabilityId)
    {
        var linking = await _teamCapabilityLinkingRepository.FindByTeamAndCapabilityIds(teamId, capabilityId);

        if (linking == null)
        {
            _logger.LogWarning(
                "Attempted to delete the link between team {teamId} and capability {capabilityId}, but could not find such linking.",
                teamId,
                capabilityId
            );
            return;
        }

        await _teamCapabilityLinkingRepository.Remove(linking.Id);
    }
}

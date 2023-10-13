using SelfService.Domain.Models;

namespace SelfService.Application;

public class TeamApplicationService : ITeamApplicationService
{
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamCapabilityAssociationRepository _teamCapabilityAssociationRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly Logger<TeamApplicationService> _logger;

    public TeamApplicationService(
        ITeamRepository teamRepository,
        ITeamCapabilityAssociationRepository teamCapabilityAssociationRepository,
        ICapabilityRepository capabilityRepository,
        Logger<TeamApplicationService> logger
    )
    {
        _teamRepository = teamRepository;
        _teamCapabilityAssociationRepository = teamCapabilityAssociationRepository;
        _capabilityRepository = capabilityRepository;
        _logger = logger;
    }

    public Task<IEnumerable<Team>> GetAllTeams()
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

        var newTeam = new Team(TeamId.Create(), name, description, createdBy, DateTime.UtcNow);
        await _teamRepository.Add(newTeam);
        return newTeam;
    }

    public Task RemoveTeam(TeamId id)
    {
        _teamRepository.Remove(id);
        return Task.CompletedTask;
    }

    public async Task AddAssociationWithCapability(TeamId teamId, CapabilityId capabilityId)
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

        var association = await _teamCapabilityAssociationRepository.FindByTeamAndCapabilityIds(teamId, capabilityId);

        if (association != null)
        {
            _logger.LogWarning(
                "Attempted to add association between team {teamId} and capability {capabilityId}, but such association already exists.",
                teamId,
                capabilityId
            );
            return;
        }

        await _teamCapabilityAssociationRepository.Add(new TeamCapabilityAssociation(teamId, capabilityId));
    }

    public async Task RemoveAssociationWithCapability(TeamId teamId, CapabilityId capabilityId)
    {
        var association = await _teamCapabilityAssociationRepository.FindByTeamAndCapabilityIds(teamId, capabilityId);

        if (association == null)
        {
            _logger.LogWarning(
                "Attempted to delete association between team {teamId} and capability {capabilityId}, but could not find such association.",
                teamId,
                capabilityId
            );
            return;
        }

        await _teamCapabilityAssociationRepository.Remove(association);
    }
}

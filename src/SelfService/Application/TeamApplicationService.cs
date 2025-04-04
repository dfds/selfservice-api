using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class TeamApplicationService : ITeamApplicationService
{
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamCapabilityLinkingRepository _teamCapabilityLinkingRepository;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly ILogger<TeamApplicationService> _logger;

    public TeamApplicationService(
        ITeamRepository teamRepository,
        ITeamCapabilityLinkingRepository teamCapabilityLinkingRepository,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        ILogger<TeamApplicationService> logger
    )
    {
        _teamRepository = teamRepository;
        _teamCapabilityLinkingRepository = teamCapabilityLinkingRepository;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public Task<List<Team>> GetAllTeams()
    {
        return _teamRepository.GetAll();
    }

    public Task<Team?> GetTeam(TeamId id)
    {
        return _teamRepository.FindById(id);
    }

    // This function exists in order for interface functions to be able to add links with their own transactional boundaries,
    // since nesting transactional boundaries does not work.
    private async Task<TeamCapabilityLink> AddLinkToCapabilityInternal(
        TeamId teamId,
        CapabilityId capabilityId,
        UserId createdBy
    )
    {
        var team = await _teamRepository.FindById(teamId);
        if (team == null)
        {
            throw new EntityNotFoundException("Team does not exist");
        }

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
        {
            throw new EntityNotFoundException("Capability does not exist");
        }

        var linking = await _teamCapabilityLinkingRepository.FindByPredicate(x =>
            x.TeamId == teamId && x.CapabilityId == capabilityId
        );

        if (linking != null)
        {
            throw new EntityAlreadyExistsException(
                $"Attempted to add a link between team {teamId} and capability {capabilityId}, but such linking already exists."
            );
        }

        var newLinking = new TeamCapabilityLink(
            TeamCapabilityLinkId.New(),
            teamId,
            capabilityId,
            createdBy,
            DateTime.UtcNow
        );
        await _teamCapabilityLinkingRepository.Add(newLinking);
        return newLinking;
    }

    [TransactionalBoundary]
    public async Task<Team> AddTeam(
        string name,
        string description,
        UserId createdBy,
        List<CapabilityId> linkedCapabilityIds
    )
    {
        var teamWithThisName = await _teamRepository.FindByPredicate(x => x.Name == name);
        if (teamWithThisName != null)
        {
            throw new ArgumentException(
                $"Team with name {name} already exists, please use the other team or choose a different name."
            );
        }

        var newTeam = new Team(TeamId.New(), name, description, createdBy, DateTime.UtcNow);
        await _teamRepository.Add(newTeam);

        foreach (var capabilityId in linkedCapabilityIds)
        {
            await AddLinkToCapabilityInternal(newTeam.Id, capabilityId, createdBy);
        }

        return newTeam;
    }

    [TransactionalBoundary]
    public Task RemoveTeam(TeamId id)
    {
        _teamRepository.Remove(id);
        return Task.CompletedTask;
    }

    [TransactionalBoundary]
    public async Task<TeamCapabilityLink> AddLinkToCapability(
        TeamId teamId,
        CapabilityId capabilityId,
        UserId createdBy
    )
    {
        return await AddLinkToCapabilityInternal(teamId, capabilityId, createdBy);
    }

    [TransactionalBoundary]
    public async Task RemoveLinkToCapability(TeamId teamId, CapabilityId capabilityId)
    {
        var linking = await _teamCapabilityLinkingRepository.FindByPredicate(x =>
            x.TeamId == teamId && x.CapabilityId == capabilityId
        );

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

    public async Task<List<Team>> GetLinkedTeams(CapabilityId capabilityId)
    {
        List<Team> linkedTeams = new List<Team>();
        var links = await _teamCapabilityLinkingRepository.GetAllWithPredicate(x => x.CapabilityId == capabilityId);

        foreach (var teamCapabilityLink in links)
        {
            var team = await _teamRepository.FindById(teamCapabilityLink.TeamId);
            if (team == null)
            {
                _logger.LogWarning(
                    "Found a link between team {teamId} and capability {capabilityId}, but could not find the team.",
                    teamCapabilityLink.TeamId,
                    capabilityId
                );
                continue;
            }

            linkedTeams.Add(team);
        }

        return linkedTeams;
    }

    public async Task<List<Capability>> GetLinkedCapabilities(TeamId teamId)
    {
        List<Capability> linkedCapabilities = new List<Capability>();
        var links = await _teamCapabilityLinkingRepository.GetAllWithPredicate(x => x.TeamId == teamId);

        foreach (var teamCapabilityLink in links)
        {
            var capability = await _capabilityRepository.FindBy(teamCapabilityLink.CapabilityId);
            if (capability == null)
            {
                _logger.LogWarning(
                    "Found a link between team {teamId} and capability {capabilityId}, but could not find the capability.",
                    teamId,
                    teamCapabilityLink.CapabilityId
                );
                continue;
            }

            linkedCapabilities.Add(capability);
        }

        return linkedCapabilities;
    }

    public async Task<IEnumerable<UserId>> GetMembers(TeamId teamId)
    {
        var links = await _teamCapabilityLinkingRepository.GetAllWithPredicate(x => x.TeamId == teamId);

        HashSet<UserId> userIds = new HashSet<UserId>();
        foreach (var link in links)
        {
            var members = await _membershipRepository.FindBy(link.CapabilityId);
            members.ToList().ForEach(x => userIds.Add(x.UserId));
        }

        return userIds.ToList();
    }
}

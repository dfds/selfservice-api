using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Builders;

public class TeamApplicationServiceBuilder
{
    private ITeamRepository _teamRepository;
    private ITeamCapabilityLinkingRepository _teamCapabilityLinkingRepository;
    private ICapabilityRepository _capabilityRepository;
    private ILogger<TeamApplicationService> _logger;

    public TeamApplicationServiceBuilder()
    {
        _teamRepository = Dummy.Of<ITeamRepository>();
        _teamCapabilityLinkingRepository = Dummy.Of<ITeamCapabilityLinkingRepository>();
        _capabilityRepository = Dummy.Of<ICapabilityRepository>();
        _logger = Dummy.Of<ILogger<TeamApplicationService>>();
    }

    public TeamApplicationServiceBuilder WithTeamRepository(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
        return this;
    }

    public TeamApplicationServiceBuilder WithTeamCapabilityLinkingRepository(
        ITeamCapabilityLinkingRepository teamCapabilityLinkingRepository
    )
    {
        _teamCapabilityLinkingRepository = teamCapabilityLinkingRepository;
        return this;
    }

    public TeamApplicationServiceBuilder WithDbContextAndDefaultRepositories(SelfServiceDbContext dbContext)
    {
        _teamRepository = new TeamRepository(dbContext);
        _teamCapabilityLinkingRepository = new TeamCapabilityLinkingRepository(dbContext);
        _capabilityRepository = new CapabilityRepository(dbContext);
        return this;
    }

    public TeamApplicationServiceBuilder WithCapabilityRepository(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
        return this;
    }

    public TeamApplicationServiceBuilder WithLogger(ILogger<TeamApplicationService> logger)
    {
        _logger = logger;
        return this;
    }

    public TeamApplicationService Build()
    {
        return new(
            _teamRepository,
            _teamCapabilityLinkingRepository,
            _capabilityRepository,
            _logger,
            Dummy.Of<SystemTime>()
        );
    }
}
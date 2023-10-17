using Microsoft.Extensions.Logging;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Tests.Application;

public class TestTeamApplicationService
{
    private const string TestTeamName = "Test Name";
    private const string TestTeamDescription = "Test Description";

    [Fact]
    public async Task adding_capability_links_when_adding_team()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();
        var teamService = A.TeamApplicationService
            .WithDbContextAndDefaultRepositories(dbContext)
            .WithCapabilityRepository(capabilityRepo)
            .Build();

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        await teamService.AddTeam(
            TestTeamName,
            TestTeamDescription,
            A.UserId,
            new List<CapabilityId>() { testCapability.Id }
        );
        await dbContext.SaveChangesAsync();

        var linkedTeams = await teamService.GetLinkedTeams(testCapability.Id);
        Assert.Single(linkedTeams);

        // able to remove link without exception
        await teamService.RemoveLinkToCapability(linkedTeams[0].Id, testCapability.Id);
        await dbContext.SaveChangesAsync();

        var linkedTeamsAfterDeletion = await teamService.GetLinkedTeams(testCapability.Id);
        Assert.Empty(linkedTeamsAfterDeletion);
    }

    [Fact]
    public async Task adding_links_after_team_creation()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();
        var teamService = A.TeamApplicationService
            .WithDbContextAndDefaultRepositories(dbContext)
            .WithCapabilityRepository(capabilityRepo)
            .Build();

        await teamService.AddTeam(TestTeamName, TestTeamDescription, A.UserId, new List<CapabilityId>());
        await dbContext.SaveChangesAsync();

        var teams = await teamService.GetAllTeams();
        Assert.Single(teams);
        var testTeam = teams[0];

        var testCapability = A.Capability.Build();
        await capabilityRepo.Add(testCapability);
        await dbContext.SaveChangesAsync();

        var linkedTeams = await teamService.GetLinkedTeams(testCapability.Id);
        Assert.Empty(linkedTeams);

        await teamService.AddLinkToCapability(testTeam.Id, testCapability.Id);
        await dbContext.SaveChangesAsync();

        var linkedTeamsAfterAdding = await teamService.GetLinkedTeams(testCapability.Id);
        Assert.Single(linkedTeamsAfterAdding);
        Assert.Equal(testTeam.Id, linkedTeamsAfterAdding[0].Id);
    }
}

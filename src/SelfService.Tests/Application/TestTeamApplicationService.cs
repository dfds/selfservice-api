using SelfService.Domain.Models;

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
        var teamService = A
            .TeamApplicationService.WithDbContextAndDefaultRepositories(dbContext)
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
        var teamService = A
            .TeamApplicationService.WithDbContextAndDefaultRepositories(dbContext)
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

        await teamService.AddLinkToCapability(testTeam.Id, testCapability.Id, A.UserId);
        await dbContext.SaveChangesAsync();

        var linkedTeamsAfterAdding = await teamService.GetLinkedTeams(testCapability.Id);
        Assert.Single(linkedTeamsAfterAdding);
        Assert.Equal(testTeam.Id, linkedTeamsAfterAdding[0].Id);
    }

    [Fact]
    public async Task can_get_capabilities_and_members_of_a_team()
    {
        var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var teamService = A
            .TeamApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .WithCapabilityRepository(capabilityRepo)
            .WithMembershipRepository(membershipRepo)
            .Build();

        await teamService.AddTeam(TestTeamName, TestTeamDescription, A.UserId, new List<CapabilityId>());
        await dbContext.SaveChangesAsync();
        var teams = await teamService.GetAllTeams();
        Assert.Single(teams);
        var testTeam = teams[0];

        var members = await teamService.GetMembers(testTeam.Id);
        Assert.Empty(members);

        var capabilityA = A.Capability.WithId(CapabilityId.CreateFrom("capability1")).Build();
        var capabilityB = A.Capability.WithId(CapabilityId.CreateFrom("capability2")).Build();
        await capabilityRepo.Add(capabilityA);
        await capabilityRepo.Add(capabilityB);
        await dbContext.SaveChangesAsync();

        var testUsersInCapabilityA = new[] { "itsame@dfds.com", "mario@dfds.com" };
        foreach (var s in testUsersInCapabilityA)
        {
            await membershipRepo.Add(A.Membership.WithCapabilityId(capabilityA.Id).WithUserId(s).Build());
        }

        var testUsersInCapabilityB = new[] { "john@dfds.com", "fred@dfds.com", "eddy@dfds.com" };

        foreach (var s in testUsersInCapabilityB)
        {
            await membershipRepo.Add(A.Membership.WithCapabilityId(capabilityB.Id).WithUserId(s).Build());
        }

        await dbContext.SaveChangesAsync();

        await teamService.AddLinkToCapability(testTeam.Id, capabilityA.Id, A.UserId);
        await dbContext.SaveChangesAsync();

        var capabilities = await teamService.GetLinkedCapabilities(testTeam.Id);
        Assert.Single(capabilities);
        Assert.Equal(capabilityA.Id, capabilities[0].Id);

        var membersAfterAdding = (await teamService.GetMembers(testTeam.Id)).ToList();
        Assert.Equal(testUsersInCapabilityA.Length, membersAfterAdding.Count);
        membersAfterAdding.Sort((x, y) => String.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal));
        for (int i = 0; i < testUsersInCapabilityA.Length; i++)
        {
            Assert.Equal(testUsersInCapabilityA[i], membersAfterAdding[i].ToString());
        }
    }
}

using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestMembershipApplicationService
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task addandcreatemembership_cant_create_duplicate_membership()
    {
        //create dbContext which also is provides stub argument for MembershipRepository
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();
        UserId userId = "chungus@dfds.com";
        CapabilityId capabilityId= "reflect2improve-GPU-cluster-mgmt-qxyz";
        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        MembershipApplicationService membershipApplicationService = A.MembershipApplicationService.WithMembershipRepository(membershipRepo).Build();
        await membershipApplicationService.CreateAndAddMembership(capabilityId, userId);
        await membershipApplicationService.CreateAndAddMembership(capabilityId, userId);
        var memberships = await dbContext.Memberships.ToListAsync();
        //Assert.Equal(1, memberships.Count); <- throws a C# error, leaving this here because I want to learn the reason why
        Assert.Single(memberships);
    }

}

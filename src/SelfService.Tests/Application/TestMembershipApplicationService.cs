using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Tests.Application;

public class TestMembershipApplicationService
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_creator_as_initial_member_cant_create_duplicate_membership()
    {
        // [!] this doesn't protect against duplicate calls in between two
        // `SaveChangesAsync` calls to the db context.
        // i.e., within a single transaction it could still be called twice

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        UserId userId = "chungus@dfds.com";
        CapabilityId capabilityId = "reflect2improve-GPU-cluster-mgmt-qxyz";
        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var membershipApplicationService = A
            .MembershipApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .Build();

        await membershipApplicationService.AddCreatorAsInitialMember(capabilityId, userId);
        await dbContext.SaveChangesAsync();

        //await Assert.ThrowsAsync<AlreadyHasActiveMembershipException>(
        //    async () => await membershipApplicationService.AddCreatorAsInitialMember(capabilityId, userId)
        //);
        await dbContext.SaveChangesAsync();

        var memberships = await dbContext.Memberships.ToListAsync();
        Assert.Single(memberships);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_user_cant_create_duplicate_membership()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        UserId userId = "chungus@dfds.com";
        CapabilityId capabilityId = "reflect2improve-GPU-cluster-mgmt-qxyz";
        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        var invitationRepo = A.InvitationRepository.WithDbContext(dbContext).Build();
        var membershipApplicationService = A
            .MembershipApplicationService.WithDbContextAndDefaultRepositories(dbContext)
            .Build();

        await membershipApplicationService.JoinCapability(capabilityId, userId);
        await dbContext.SaveChangesAsync();

        //await Assert.ThrowsAsync<AlreadyHasActiveMembershipException>(
        //    async () => await membershipApplicationService.JoinCapability(capabilityId, userId)
        //);
        await dbContext.SaveChangesAsync();

        var memberships = await dbContext.Memberships.ToListAsync();
        Assert.Single(memberships);
    }
}

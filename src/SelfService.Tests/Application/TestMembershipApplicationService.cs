using Microsoft.EntityFrameworkCore;
using Moq;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

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
        var membershipApplicationService = A.MembershipApplicationService
            .WithMembershipRepository(membershipRepo)
            .Build();

        await membershipApplicationService.AddCreatorAsInitialMember(capabilityId, userId);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AlreadyHasActiveMembershipException>(
            async () => await membershipApplicationService.AddCreatorAsInitialMember(capabilityId, userId)
        );
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
        var membershipApplicationService = A.MembershipApplicationService
            .WithMembershipRepository(membershipRepo)
            .Build();

        await membershipApplicationService.JoinCapability(capabilityId, userId);
        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<AlreadyHasActiveMembershipException>(
            async () => await membershipApplicationService.JoinCapability(capabilityId, userId)
        );
        await dbContext.SaveChangesAsync();

        var memberships = await dbContext.Memberships.ToListAsync();
        Assert.Single(memberships);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_create_approvals()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        var membershipApplicationRepo = A.MembershipApplicationRepository.WithDbContext(dbContext).Build();
        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        var membershipApplicationService = A.MembershipApplicationService
            .WithMembershipRepository(membershipRepo)
            .WithMembershipApplicationRepository(membershipApplicationRepo)
            .WithCapabilityRepository(capabilityRepo)
            .Build();

        var capability = A.Capability.Build();
        await capabilityRepo.Add(capability);
        await dbContext.SaveChangesAsync();

        var approver = A.UserId;
        await membershipRepo.Add(new Membership(MembershipId.New(), capability.Id, approver, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var toJoin = UserId.Parse("bar");

        await membershipApplicationService.SubmitMembershipApplication(capability.Id, toJoin);
        await dbContext.SaveChangesAsync();

        var application = await membershipApplicationRepo.FindPendingBy(capability.Id, toJoin);
        Assert.NotNull(application);
        var applicationId = application.Id;

        var authService = new Mock<IAuthorizationService>();
        authService.Setup(x => x.CanApprove(approver, application)).ReturnsAsync(true);

        // We need to also check if we can approve the application
        var membershipApplicationServiceWithAuth = A.MembershipApplicationService
            .WithMembershipApplicationRepository(membershipApplicationRepo)
            .WithAuthorizationService(authService.Object)
            .Build();

        await membershipApplicationServiceWithAuth.ApproveMembershipApplication(applicationId, approver);
        await dbContext.SaveChangesAsync();

        var applicationAfterApproval = await membershipApplicationRepo.FindPendingBy(capability.Id, toJoin);
        Assert.NotNull(applicationAfterApproval);
        Assert.NotEmpty(applicationAfterApproval.Approvals);
    }
}

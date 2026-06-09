using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SelfService.Application;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
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

    [Fact]
    public async Task submit_membership_application_auto_finalizes_when_capability_has_no_members()
    {
        CapabilityId capabilityId = "cap-a";
        UserId userId = "user@dfds.com";

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(x => x.Exists(capabilityId)).ReturnsAsync(true);

        var membershipRepo = new Mock<IMembershipRepository>();
        membershipRepo
            .Setup(x => x.GetAllWithPredicate(It.IsAny<Func<Membership, bool>>()))
            .ReturnsAsync(new List<Membership>());

        var membershipApplicationRepo = new Mock<IMembershipApplicationRepository>();
        membershipApplicationRepo.Setup(x => x.FindPendingBy(capabilityId, userId)).ReturnsAsync((MembershipApplication?)null);

        MembershipApplication? added = null;
        membershipApplicationRepo
            .Setup(x => x.Add(It.IsAny<MembershipApplication>()))
            .Callback<MembershipApplication>(x => added = x)
            .Returns(Task.CompletedTask);

        var rbacService = new Mock<IRbacApplicationService>();
        rbacService
            .Setup(x => x.GetAssignableRoles())
            .ReturnsAsync(
                new List<RbacRole>
                {
                    new(
                        RbacRoleId.New(),
                        ownerId: "owner@dfds.com",
                        createdAt: DateTime.UtcNow,
                        updatedAt: DateTime.UtcNow,
                        name: "Owner",
                        description: "Owner role",
                        type: RbacAccessType.Capability
                    ),
                }
            );

        var service = new MembershipApplicationService(
            logger: NullLogger<MembershipApplicationService>.Instance,
            capabilityRepository: capabilityRepo.Object,
            membershipRepository: membershipRepo.Object,
            membershipApplicationRepository: membershipApplicationRepo.Object,
            authorizationService: Mock.Of<IAuthorizationService>(),
            rbacApplicationService: rbacService.Object,
            systemTime: SystemTime.Default,
            membershipQuery: Mock.Of<IMembershipQuery>(),
            membershipApplicationDomainService: Mock.Of<IMembershipApplicationDomainService>(),
            myCapabilitiesQuery: Mock.Of<IMyCapabilitiesQuery>()
        );

        await service.SubmitMembershipApplication(capabilityId, userId);

        Assert.NotNull(added);
        Assert.True(added!.IsFinalized);
        rbacService.Verify(x => x.GrantRoleGrant(userId.ToString(), It.IsAny<RbacRoleGrant>()), Times.Once);
    }
}

using Microsoft.EntityFrameworkCore;
using Moq;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Comparers;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestMemberCleaner
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task deactivated_member_cleaner_removes_deactivated_users()
    {
        //create dbContext which also provides stub argument for MembershipRepository
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        SystemTime systemTime = SystemTime.Default;
        var membershipCleaner = A
            .DeactivatedMemberCleanerApplicationService.WithMemberRepository(new MemberRepository(dbContext))
            .WithMembershipRepository(new MembershipRepository(dbContext))
            .WithMembershipApplicationRepository(new MembershipApplicationRepository(dbContext, systemTime))
            .Build();

        var capability = A.Capability.Build();

        var memberActive = A.Membership.WithCapabilityId(capability.Id).WithUserId("useractive@dfds.com").Build();
        var memberDeactivated = A
            .Membership.WithCapabilityId(capability.Id)
            .WithUserId("userdeactivated@dfds.com")
            .Build();
        var memberNotfound1 = A
            .Membership.WithCapabilityId(capability.Id)
            .WithUserId("usernotinazure1@dfds.com")
            .Build();
        var memberNotfound2 = A
            .Membership.WithCapabilityId(capability.Id)
            .WithUserId("usernotinazure2@dfds.com")
            .Build();
        var memberNotfound3 = A
            .Membership.WithCapabilityId(capability.Id)
            .WithUserId("usernotinazure2@dfds.com")
            .Build();

        var repo = A.MembershipRepository.WithDbContext(dbContext).Build();

        await repo.Add(memberActive);
        await repo.Add(memberDeactivated);
        await repo.Add(memberNotfound1);
        await repo.Add(memberNotfound2);
        await repo.Add(memberNotfound3);

        await dbContext.SaveChangesAsync();

        // create stub/mock
        var userStatusChecker = new StubUserStatusChecker().WithDeactivatedUser(memberDeactivated.UserId);
        await membershipCleaner.RemoveDeactivatedMemberships(userStatusChecker);

        var remaining = await dbContext.Memberships.ToListAsync();
        Assert.Contains(memberActive, remaining, new MembershipComparer());
        Assert.Contains(memberNotfound1, remaining, new MembershipComparer());
        Assert.Contains(memberNotfound2, remaining, new MembershipComparer());
        Assert.Contains(memberNotfound3, remaining, new MembershipComparer());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task deactivated_member_cleaner_respects_referential_integrity()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var deactivatedMember = A.Member.WithUserId("userdeactivated@dfds.com").Build();
        var approverMember = A.Member.WithUserId("useractive@dfds.com").Build();

        var memberRepo = new MemberRepository(dbContext);
        await memberRepo.Add(deactivatedMember);
        await memberRepo.Add(approverMember);
        await dbContext.SaveChangesAsync();

        var membershipApplicationRepo = A.MembershipApplicationRepository.WithDbContext(dbContext).Build();
        var membershipRepo = A.MembershipRepository.WithDbContext(dbContext).Build();
        var capabilityRepo = A.CapabilityRepository.WithDbContext(dbContext).Build();
        var membershipApplicationService = A
            .MembershipApplicationService.WithMembershipRepository(membershipRepo)
            .WithMembershipApplicationRepository(membershipApplicationRepo)
            .WithCapabilityRepository(capabilityRepo)
            .Build();

        var capability = A.Capability.Build();
        await capabilityRepo.Add(capability);
        await dbContext.SaveChangesAsync();

        await membershipRepo.Add(new Membership(MembershipId.New(), capability.Id, approverMember.Id, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        await membershipApplicationService.SubmitMembershipApplication(capability.Id, deactivatedMember.Id);
        await dbContext.SaveChangesAsync();

        var application = await membershipApplicationRepo.FindPendingBy(capability.Id, deactivatedMember.Id);
        Assert.NotNull(application);
        var applicationId = application.Id;

        var authService = new Mock<IAuthorizationService>();
        authService.Setup(x => x.CanApprove(approverMember.Id, application)).ReturnsAsync(true);

        // We need to also check if we can approve the application
        var membershipApplicationServiceWithAuth = A
            .MembershipApplicationService.WithMembershipApplicationRepository(membershipApplicationRepo)
            .WithAuthorizationService(authService.Object)
            .Build();

        await membershipApplicationServiceWithAuth.ApproveMembershipApplication(applicationId, approverMember.Id);
        await dbContext.SaveChangesAsync();

        var applicationAfterApproval = await membershipApplicationRepo.FindPendingBy(
            capability.Id,
            deactivatedMember.Id
        );
        Assert.NotNull(applicationAfterApproval);
        Assert.NotEmpty(applicationAfterApproval.Approvals);

        //now the repository is in the right state

        var membershipCleaner = A
            .DeactivatedMemberCleanerApplicationService.WithMemberRepository(memberRepo)
            .WithMembershipRepository(membershipRepo)
            .WithMembershipApplicationRepository(membershipApplicationRepo)
            .WithInvitationRepository(A.InvitationRepository.WithDbContext(dbContext).Build())
            .Build();

        var userStatusChecker = new StubUserStatusChecker()
            .WithDeactivatedUser(deactivatedMember.Id)
            .WithActiveUser(approverMember.Id);
        await membershipCleaner.RemoveDeactivatedMemberships(userStatusChecker);
        await dbContext.SaveChangesAsync();

        var remainingApplications = await membershipApplicationRepo.FindBy(applicationId);
        Assert.Null(remainingApplications);

        // CLEAN UP
        await memberRepo.Remove(approverMember.Id);
        dbContext.Capabilities.Remove(capability);
        await dbContext.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using SelfService.Tests.Comparers;
using SelfService.Tests.TestDoubles;
using Microsoft.Extensions.Logging; //for our own homemade things
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Domain;
using SelfService.Domain.Models;

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
        var membershipCleaner = A.DeactivatedMemberCleanerApplicationService
            .WithMemberRepository(new MemberRepository(dbContext))
            .WithMembershipRepository(new MembershipRepository(dbContext))
            .WithMembershipApplicationRepository(new MembershipApplicationRepository(dbContext, systemTime))
            .Build();

        var capability = A.Capability.Build();

        var memberActive = A.Membership.WithCapabilityId(capability.Id).WithUserId("useractive@dfds.com").Build();
        var memberDeactivated = A.Membership
            .WithCapabilityId(capability.Id)
            .WithUserId("userdeactivated@dfds.com")
            .Build();
        var memberNotfound1 = A.Membership
            .WithCapabilityId(capability.Id)
            .WithUserId("usernotinazure1@dfds.com")
            .Build();
        var memberNotfound2 = A.Membership
            .WithCapabilityId(capability.Id)
            .WithUserId("usernotinazure2@dfds.com")
            .Build();
        var memberNotfound3 = A.Membership
            .WithCapabilityId(capability.Id)
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
        var userStatusChecker = new StubUserStatusChecker();
        await membershipCleaner.RemoveDeactivatedMemberships(userStatusChecker);

        var remaining = await dbContext.Memberships.ToListAsync();
        Assert.Contains(memberActive, remaining, new MembershipComparer());
        Assert.Contains(memberNotfound1, remaining, new MembershipComparer());
        Assert.Contains(memberNotfound2, remaining, new MembershipComparer());
        Assert.Contains(memberNotfound3, remaining, new MembershipComparer());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task deactivated_member_cleaner_respects_referential_integrity()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        SystemTime systemTime = SystemTime.Default;

        // create memberships that will be looped over:
        var deactivatedMember = A.Member.WithUserId("userdeactivated@dfds.com").Build();

        var memberRepo = new MemberRepository(dbContext);
        await memberRepo.Add(deactivatedMember);

        await dbContext.Members.AddAsync(deactivatedMember, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync();

        //create membership application for the test
        var Id = Guid.NewGuid();

        var membershipApplication1 = A.MembershipApplication
            .WithApplicant("userdeactivated@dfds.com")
            .WithId(Id)
            .WithApproval(builder => builder.WithApprovedBy("useractive@dfds.com").WithMembershipApplicationId(Id))
            .Build();

        var membershipApplicationRepo = A.MembershipApplicationRepository.WithDbContext(dbContext).Build();
        await membershipApplicationRepo.Add(membershipApplication1);

        await dbContext.SaveChangesAsync();
        //now the repository is in the right state

        var membershipCleaner = A.DeactivatedMemberCleanerApplicationService
            .WithMemberRepository(memberRepo)
            .WithMembershipRepository(new MembershipRepository(dbContext))
            .WithMembershipApplicationRepository(membershipApplicationRepo)
            .Build();

        var userStatusChecker = new StubUserStatusChecker();
        await membershipCleaner.RemoveDeactivatedMemberships(userStatusChecker);

        await dbContext.SaveChangesAsync();

        var remainingApplications = await dbContext.MembershipApplications.ToListAsync();
        Assert.Empty(remainingApplications);
    }
}

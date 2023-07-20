using Microsoft.EntityFrameworkCore;
using SelfService.Tests.Comparers;
using SelfService.Tests.TestDoubles;
using Microsoft.Extensions.Logging; //for our own homemade things
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Domain;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestMembershipRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cancel_removes_membership_from_capability_with_multiple_members()
    {
        //setup
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var capability = A.Capability.Build();
        var memberA = A.Membership.WithCapabilityId(capability.Id).WithUserId("MA").Build();
        var memberB = A.Membership.WithCapabilityId(capability.Id).WithUserId("MB").Build();

        var repo = A.MembershipRepository
            .WithDbContext(dbContext)
            .Build();

        await repo.Add(memberA);
        await repo.Add(memberB);

        await dbContext.SaveChangesAsync();

        //tests and assertions
        var inserted = await dbContext.Memberships.ToListAsync();
        Assert.Contains(memberA, inserted, new MembershipComparer());
        Assert.Contains(memberB, inserted, new MembershipComparer());

        await repo.CancelWithCapabilityId(memberA.CapabilityId, memberA.UserId);
        await dbContext.SaveChangesAsync();

        var remaining = await dbContext.Memberships.ToListAsync();
        Assert.Contains(memberB, remaining, new MembershipComparer());
        Assert.DoesNotContain(memberA, remaining, new MembershipComparer());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cancel_keeps_membership_for_capability_with_single_member()
    {
        //setup
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var capability = A.Capability.Build();
        var member = A.Membership.WithCapabilityId(capability.Id).Build();

        var repo = A.MembershipRepository
            .WithDbContext(dbContext)
            .Build();

        await repo.Add(member);

        await dbContext.SaveChangesAsync();

        //tests and assertions
        var inserted = await dbContext.Memberships.ToListAsync();
        Assert.Contains(member, inserted, new MembershipComparer());

        await repo.CancelWithCapabilityId(member.CapabilityId, member.UserId);
        await dbContext.SaveChangesAsync();

        var remaining = await dbContext.Memberships.ToListAsync();
        Assert.Contains(member, remaining, new MembershipComparer());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task deactivated_member_cleaner_removes_deactivated_users(){

        //create dbContext which also is provides stub argument for MembershipRepository
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        SystemTime systemTime = SystemTime.Default;
        var membershipCleaner = A.DeactivatedMemberCleanerApplicationService
            .WithMemberRepository(new MemberRepository(dbContext))
            .WithMembershipRepository(new MembershipRepository(dbContext))
            .WithMembershipApplicationRepository(new MembershipApplicationRepository(dbContext, systemTime))
            .Build();

        var logger = LoggerFactory.Create(loggerConfig =>
                    {
                        loggerConfig
                            .AddConsole()
                            .AddFilter(DbLoggerCategory.Database.Command.Name, LogLevel.Information);
                    }).CreateLogger<RemoveDeactivatedMemberships>();

        var capability = A.Capability.Build();

        var member_active = A.Membership.WithCapabilityId(capability.Id).WithUserId("useractive@dfds.com").Build();
        var member_deact = A.Membership.WithCapabilityId(capability.Id).WithUserId("userdeactivated@dfds.com").Build();
        var member_notfound1 = A.Membership.WithCapabilityId(capability.Id).WithUserId("usernotinazure1@dfds.com").Build();
        var member_notfound2 = A.Membership.WithCapabilityId(capability.Id).WithUserId("usernotinazure2@dfds.com").Build();
        var member_notfound3 = A.Membership.WithCapabilityId(capability.Id).WithUserId("usernotinazure2@dfds.com").Build();

        var repo = A.MembershipRepository
            .WithDbContext(dbContext)
            .Build();

        await repo.Add(member_active);
        await repo.Add(member_deact);
        await repo.Add(member_notfound1);
        await repo.Add(member_notfound2);
        await repo.Add(member_notfound3);

        await dbContext.SaveChangesAsync();

        // create stub/mock
        var userStatusChecker = new StubUserStatusChecker(logger);
        await membershipCleaner.RemoveDeactivatedMemberships(userStatusChecker);

        var remaining = await dbContext.Memberships.ToListAsync();
        Assert.Contains(member_active, remaining, new MembershipComparer());
        // [TRIAL] currently we do not delete users for which 404 happened in Azure AD
        Assert.Contains(member_notfound1, remaining, new MembershipComparer());
        Assert.Contains(member_notfound2, remaining, new MembershipComparer());
        Assert.Contains(member_notfound3, remaining, new MembershipComparer());
    }
}

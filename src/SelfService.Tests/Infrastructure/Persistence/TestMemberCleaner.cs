using Microsoft.EntityFrameworkCore;
using SelfService.Tests.Comparers;
using SelfService.Tests.TestDoubles;
using Microsoft.Extensions.Logging; //for our own homemade things
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Domain;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestMemberCleaner {
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
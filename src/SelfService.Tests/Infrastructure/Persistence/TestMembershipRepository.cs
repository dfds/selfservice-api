using Microsoft.EntityFrameworkCore;
using SelfService.Tests.Comparers;

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
}

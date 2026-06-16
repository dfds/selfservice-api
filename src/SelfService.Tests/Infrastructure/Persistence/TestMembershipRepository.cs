using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SelfService.Domain;
using SelfService.Domain.Models;
using SelfService.Infrastructure.BackgroundJobs;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Comparers;
using SelfService.Tests.TestDoubles;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestMembershipRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cancel_removes_membership_from_capability_with_multiple_members()
    {
        //setup
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capability = A.Capability.Build();
        var memberA = A.Membership.WithCapabilityId(capability.Id).WithUserId("MA").Build();
        var memberB = A.Membership.WithCapabilityId(capability.Id).WithUserId("MB").Build();

        var repo = A.MembershipRepository.WithDbContext(dbContext).Build();

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
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capability = A.Capability.Build();
        var member = A.Membership.WithCapabilityId(capability.Id).Build();

        var repo = A.MembershipRepository.WithDbContext(dbContext).Build();

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
    public async Task get_member_counts_by_capability_ids_returns_count_per_capability()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capA = A.Capability.Build();
        var capB = A.Capability.Build();
        var repo = A.MembershipRepository.WithDbContext(dbContext).Build();

        await repo.Add(A.Membership.WithCapabilityId(capA.Id).WithUserId("U1").Build());
        await repo.Add(A.Membership.WithCapabilityId(capA.Id).WithUserId("U2").Build());
        await repo.Add(A.Membership.WithCapabilityId(capB.Id).WithUserId("U1").Build());
        await dbContext.SaveChangesAsync();

        var counts = await repo.GetMemberCountsByCapabilityIds(new[] { capA.Id, capB.Id });

        Assert.Equal(2, counts[capA.Id]);
        Assert.Equal(1, counts[capB.Id]);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task get_member_counts_by_capability_ids_empty_input_returns_empty()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();
        var repo = A.MembershipRepository.WithDbContext(dbContext).Build();

        var counts = await repo.GetMemberCountsByCapabilityIds(Array.Empty<CapabilityId>());

        Assert.Empty(counts);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task get_all_memberships_for_user_ids_returns_memberships_for_requested_users()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capA = A.Capability.Build();
        var capB = A.Capability.Build();
        var repo = A.MembershipRepository.WithDbContext(dbContext).Build();

        var u1 = UserId.Parse("U1");
        var u2 = UserId.Parse("U2");
        var u3 = UserId.Parse("U3");
        await repo.Add(A.Membership.WithCapabilityId(capA.Id).WithUserId("U1").Build());
        await repo.Add(A.Membership.WithCapabilityId(capB.Id).WithUserId("U1").Build());
        await repo.Add(A.Membership.WithCapabilityId(capA.Id).WithUserId("U2").Build());
        await repo.Add(A.Membership.WithCapabilityId(capA.Id).WithUserId("U3").Build());
        await dbContext.SaveChangesAsync();

        var memberships = await repo.GetAllMembershipsForUserIds(new[] { u1, u2 });

        Assert.Equal(3, memberships.Count);
        Assert.All(memberships, m => Assert.Contains(m.UserId, new[] { u1, u2 }));
        Assert.DoesNotContain(memberships, m => m.UserId == u3);
    }
}

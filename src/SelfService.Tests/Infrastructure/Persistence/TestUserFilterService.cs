using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Metrics;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Builders;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestUserFilterService
{
    private static string FilterAudience(params object[] filters) =>
        JsonSerializer.Serialize(new { mode = "filter", filters });

    private static CapabilityCosts CostsFor(Capability capability, params float[] dailyValues)
    {
        var baseDate = new DateTime(2026, 6, 1);
        var series = dailyValues.Select((v, i) => new TimeSeries(v, baseDate.AddDays(i))).ToArray();
        return new CapabilityCosts(capability.Id, series);
    }

    private static async Task<AllCapabilitiesCostsCache> CacheWith(params CapabilityCosts[] costs)
    {
        var cache = new AllCapabilitiesCostsCache();
        await cache.UpdateCache(new AllCapabilitiesCosts(costs.ToList()));
        return cache;
    }

    private static UserFilterService Sut(SelfServiceDbContext dbContext, AllCapabilitiesCostsCache cache) =>
        new(dbContext, new CapabilityFilterService(dbContext, cache));

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task capability_status_filter_selects_only_members_of_matching_capabilities()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var capActive = A
            .Capability.WithId(CapabilityId.CreateFrom("active-cap"))
            .WithStatus(CapabilityStatusOptions.Active)
            .Build();
        var capPending = A
            .Capability.WithId(CapabilityId.CreateFrom("pending-cap"))
            .WithStatus(CapabilityStatusOptions.PendingDeletion)
            .Build();
        var inActiveCap = A.Member.WithUserId(UserId.Parse("alice")).WithEmail("alice@dfds.com").Build();
        var inPendingCap = A.Member.WithUserId(UserId.Parse("bob")).WithEmail("bob@dfds.com").Build();
        var inNoCap = A.Member.WithUserId(UserId.Parse("carol")).WithEmail("carol@dfds.com").Build();

        dbContext.Capabilities.AddRange(capActive, capPending);
        dbContext.Members.AddRange(inActiveCap, inPendingCap, inNoCap);
        dbContext.Memberships.AddRange(
            A.Membership.WithCapabilityId(capActive.Id).WithUserId(inActiveCap.Id).Build(),
            A.Membership.WithCapabilityId(capPending.Id).WithUserId(inPendingCap.Id).Build()
        );
        await dbContext.SaveChangesAsync();

        var result = await Sut(dbContext, new AllCapabilitiesCostsCache())
            .ResolveUsers(
                FilterAudience(
                    new
                    {
                        field = "status",
                        @operator = "eq",
                        value = "active",
                    }
                )
            );

        Assert.Equal(new[] { inActiveCap.Id }, result.Members.Select(m => m.Id).ToArray());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task capability_cost_filter_selects_members_of_expensive_capabilities()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var expensive = A.Capability.WithId(CapabilityId.CreateFrom("expensive-cap")).Build();
        var cheap = A.Capability.WithId(CapabilityId.CreateFrom("cheap-cap")).Build();
        var onExpensive = A.Member.WithUserId(UserId.Parse("alice")).WithEmail("alice@dfds.com").Build();
        var onCheap = A.Member.WithUserId(UserId.Parse("bob")).WithEmail("bob@dfds.com").Build();

        dbContext.Capabilities.AddRange(expensive, cheap);
        dbContext.Members.AddRange(onExpensive, onCheap);
        dbContext.Memberships.AddRange(
            A.Membership.WithCapabilityId(expensive.Id).WithUserId(onExpensive.Id).Build(),
            A.Membership.WithCapabilityId(cheap.Id).WithUserId(onCheap.Id).Build()
        );
        await dbContext.SaveChangesAsync();

        var cache = await CacheWith(CostsFor(expensive, 6f, 4f), CostsFor(cheap, 1f));

        var result = await Sut(dbContext, cache)
            .ResolveUsers(
                FilterAudience(
                    new
                    {
                        field = "cost",
                        @operator = "gt",
                        value = "2",
                    }
                )
            );

        Assert.Equal(new[] { onExpensive.Id }, result.Members.Select(m => m.Id).ToArray());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task combined_user_and_capability_filters_are_anded()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var expensive = A.Capability.WithId(CapabilityId.CreateFrom("expensive-cap")).Build();
        var cheap = A.Capability.WithId(CapabilityId.CreateFrom("cheap-cap")).Build();

        // alice: @dfds + expensive  -> matches both filters
        // bob:   @dfds + cheap      -> fails cost filter
        // carol: @other + expensive -> fails email filter
        var alice = A.Member.WithUserId(UserId.Parse("alice")).WithEmail("alice@dfds.com").Build();
        var bob = A.Member.WithUserId(UserId.Parse("bob")).WithEmail("bob@dfds.com").Build();
        var carol = A.Member.WithUserId(UserId.Parse("carol")).WithEmail("carol@other.com").Build();

        dbContext.Capabilities.AddRange(expensive, cheap);
        dbContext.Members.AddRange(alice, bob, carol);
        dbContext.Memberships.AddRange(
            A.Membership.WithCapabilityId(expensive.Id).WithUserId(alice.Id).Build(),
            A.Membership.WithCapabilityId(cheap.Id).WithUserId(bob.Id).Build(),
            A.Membership.WithCapabilityId(expensive.Id).WithUserId(carol.Id).Build()
        );
        await dbContext.SaveChangesAsync();

        var cache = await CacheWith(CostsFor(expensive, 10f), CostsFor(cheap, 1f));

        var result = await Sut(dbContext, cache)
            .ResolveUsers(
                FilterAudience(
                    new
                    {
                        field = "email",
                        @operator = "contains",
                        value = "@dfds",
                    },
                    new
                    {
                        field = "cost",
                        @operator = "gt",
                        value = "2",
                    }
                )
            );

        Assert.Equal(new[] { alice.Id }, result.Members.Select(m => m.Id).ToArray());
    }

    // NOTE: the capabilityCostCentre filter (and metadata key/value capability filters) rely on
    // EF.Functions.JsonContains (PostgreSQL jsonb @>), which the SQLite-backed InMemoryDatabase
    // cannot translate — so they are not unit-tested here (the same limitation applies to the
    // CapabilityFilterService metadata filters). That path is unchanged by this work: capabilityCostCentre
    // remains in UserScopedFields and is resolved by the existing local handler.

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task user_scoped_filter_only_does_not_narrow_by_membership()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        // Neither member has any membership; a pure user-scoped filter must still match them.
        var alice = A.Member.WithUserId(UserId.Parse("alice")).WithEmail("alice@dfds.com").Build();
        var bob = A.Member.WithUserId(UserId.Parse("bob")).WithEmail("bob@other.com").Build();
        dbContext.Members.AddRange(alice, bob);
        await dbContext.SaveChangesAsync();

        var result = await Sut(dbContext, new AllCapabilitiesCostsCache())
            .ResolveUsers(
                FilterAudience(
                    new
                    {
                        field = "email",
                        @operator = "contains",
                        value = "@dfds",
                    }
                )
            );

        Assert.Equal(new[] { alice.Id }, result.Members.Select(m => m.Id).ToArray());
    }
}

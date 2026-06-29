using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Metrics;
using SelfService.Infrastructure.Persistence;
using SelfService.Tests.Builders;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestCapabilityFilterService
{
    private static string CostFilterAudience(string op, string value) =>
        JsonSerializer.Serialize(
            new
            {
                mode = "filter",
                filters = new[]
                {
                    new
                    {
                        field = "cost",
                        @operator = op,
                        value = value,
                    },
                },
            }
        );

    private static CapabilityCosts CostsFor(Capability capability, params float[] dailyValues)
    {
        // SumForLastDays takes the most recent N timestamps; the absolute dates only need to be ordered.
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

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cost_filter_greater_than_excludes_cheaper_and_costless_capabilities()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var expensive = A.Capability.WithId(CapabilityId.CreateFrom("expensive")).Build();
        var cheap = A.Capability.WithId(CapabilityId.CreateFrom("cheap")).Build();
        var noCost = A.Capability.WithId(CapabilityId.CreateFrom("nocost")).Build();
        dbContext.Capabilities.AddRange(expensive, cheap, noCost);
        await dbContext.SaveChangesAsync();

        // noCost is intentionally absent from the cache.
        var cache = await CacheWith(CostsFor(expensive, 6f, 4f), CostsFor(cheap, 1f));
        var sut = new CapabilityFilterService(dbContext, cache);

        var result = await sut.ResolveCapabilities(CostFilterAudience("gt", "2"));

        Assert.Equal(new[] { expensive.Id }, result.Select(c => c.Id).ToArray());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cost_filter_less_than_or_equal_includes_matching_and_still_excludes_costless()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var expensive = A.Capability.WithId(CapabilityId.CreateFrom("expensive")).Build();
        var cheap = A.Capability.WithId(CapabilityId.CreateFrom("cheap")).Build();
        var noCost = A.Capability.WithId(CapabilityId.CreateFrom("nocost")).Build();
        dbContext.Capabilities.AddRange(expensive, cheap, noCost);
        await dbContext.SaveChangesAsync();

        var cache = await CacheWith(CostsFor(expensive, 10f), CostsFor(cheap, 1f));
        var sut = new CapabilityFilterService(dbContext, cache);

        var result = await sut.ResolveCapabilities(CostFilterAudience("lte", "2"));

        // cheap (1) matches; expensive (10) and noCost (no data) do not.
        Assert.Equal(new[] { cheap.Id }, result.Select(c => c.Id).ToArray());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task cost_filter_with_empty_cache_narrows_audience_to_nothing()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        dbContext.Capabilities.AddRange(
            A.Capability.WithId(CapabilityId.CreateFrom("alpha")).Build(),
            A.Capability.WithId(CapabilityId.CreateFrom("beta")).Build()
        );
        await dbContext.SaveChangesAsync();

        var sut = new CapabilityFilterService(dbContext, new AllCapabilitiesCostsCache());

        var result = await sut.ResolveCapabilities(CostFilterAudience("gt", "0"));

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task no_cost_filter_leaves_costless_capabilities_untouched()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        dbContext.Capabilities.AddRange(
            A.Capability.WithId(CapabilityId.CreateFrom("alpha")).Build(),
            A.Capability.WithId(CapabilityId.CreateFrom("beta")).Build()
        );
        await dbContext.SaveChangesAsync();

        var sut = new CapabilityFilterService(dbContext, new AllCapabilitiesCostsCache());

        var audience = JsonSerializer.Serialize(new { mode = "all" });
        var result = await sut.ResolveCapabilities(audience);

        Assert.Equal(2, result.Count);
    }
}

using Microsoft.EntityFrameworkCore;
using SelfService.Tests.Comparers;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestCapabilityRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_inserts_expected_capability_into_database()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.Capability;

        var sut = A.CapabilityRepository
            .WithDbContext(dbContext)
            .Build();

        await sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.Capabilities.ToListAsync());
        Assert.Equal(stub, inserted, new CapabilityComparer());
    }
}
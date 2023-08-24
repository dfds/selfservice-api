using Microsoft.EntityFrameworkCore;
using SelfService.Tests.Comparers;
using SelfService.Domain.Models;

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

        var sut = A.CapabilityRepository.WithDbContext(dbContext).Build();

        await sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.Capabilities.ToListAsync());
        Assert.Equal(stub, inserted, new CapabilityComparer());
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task getting_pending_deletions_with_days_return_only_capabilities_ready_for_deletion()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();
        var repo = A.CapabilityRepository.WithDbContext(dbContext).Build();

        await repo.Add(A.Capability.WithId("active"));
        await repo.Add(A.Capability.WithId("pending-new").WithStatus(CapabilityStatusOptions.PendingDeletion));
        await repo.Add(
            A.Capability
                .WithId("pending-old")
                .WithStatus(CapabilityStatusOptions.PendingDeletion)
                .WithModifiedAt(DateTime.UtcNow.AddDays(-8))
        );
        await repo.Add(A.Capability.WithId("deleted").WithStatus(CapabilityStatusOptions.Deleted));

        await dbContext.SaveChangesAsync();

        var allCapabilities = await repo.GetAll(); // does not get deleted
        var readyForDeletion = await repo.GetAllPendingDeletionFor(days: 7);

        Assert.Equal(3, allCapabilities.Count());
        Assert.Single(readyForDeletion);
        Assert.Equal("pending-old", readyForDeletion.First().Id);
    }
}

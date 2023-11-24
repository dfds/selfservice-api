using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Tests.Infrastructure.Queries;

public class TestCapabilityDeletionStatusQuery
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_expected_members_for_a_capability_with_single_membership()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubCapability = A.Capability.WithStatus(CapabilityStatusOptions.PendingDeletion).Build();
        await dbContext.Capabilities.AddAsync(stubCapability, cancellationTokenSource.Token);
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var query = new CapabilityDeletionStatusQuery(dbContext);
        var res = await query.isPendingDeletion(stubCapability.Id);
        Assert.True(res);
    }
}

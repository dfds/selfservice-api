using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Tests.Infrastructure.Queries;

public class TestAadAwsSyncCapabilityQuery
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task sets_remove_users_from_group_based_on_tag_compliance()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var compliantCapability = A.Capability
            .WithId(CapabilityId.CreateFrom("compliant-capability"))
            .WithName("compliant-capability")
            .WithJsonMetadata(
                """
                {
                    "dfds.cost.centre": "1234",
                    "dfds.businessCapability": "platform",
                    "dfds.env": "prod",
                    "dfds.data.classification": "internal",
                    "dfds.service.criticality": "high",
                    "dfds.service.availability": "24x7"
                }
                """
            )
            .Build();

        var nonCompliantCapability = A.Capability
            .WithId(CapabilityId.CreateFrom("non-compliant-capability"))
            .WithName("non-compliant-capability")
            .WithJsonMetadata("{}")
            .Build();

        await dbContext.Capabilities.AddRangeAsync(
            new[] { compliantCapability, nonCompliantCapability },
            cancellationTokenSource.Token
        );
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new AadAwsSyncCapabilityQuery(dbContext);
        var result = (await sut.GetCapabilities()).ToDictionary(x => x.Id);

        Assert.False(result[compliantCapability.Id].RemoveUsersFromGroup);
        Assert.True(result[nonCompliantCapability.Id].RemoveUsersFromGroup);
    }
}

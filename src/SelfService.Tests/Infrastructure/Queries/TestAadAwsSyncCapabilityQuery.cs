using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence.Queries;

namespace SelfService.Tests.Infrastructure.Queries;

public class TestAadAwsSyncCapabilityQuery
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task returns_empty_members_for_non_compliant_capabilities()
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

        var compliantMember = A.Member.WithUserId(UserId.Parse("compliant-user")).Build();
        var nonCompliantMember = A.Member.WithUserId(UserId.Parse("non-compliant-user")).Build();

        var compliantMembership = A.Membership
            .WithCapabilityId(compliantCapability.Id)
            .WithUserId(compliantMember.Id)
            .Build();

        var nonCompliantMembership = A.Membership
            .WithCapabilityId(nonCompliantCapability.Id)
            .WithUserId(nonCompliantMember.Id)
            .Build();

        await dbContext.Capabilities.AddRangeAsync(
            new[] { compliantCapability, nonCompliantCapability },
            cancellationTokenSource.Token
        );
        await dbContext.Members.AddRangeAsync(new[] { compliantMember, nonCompliantMember }, cancellationTokenSource.Token);
        await dbContext.Memberships.AddRangeAsync(
            new[] { compliantMembership, nonCompliantMembership },
            cancellationTokenSource.Token
        );
        await dbContext.SaveChangesAsync(cancellationTokenSource.Token);

        var sut = new AadAwsSyncCapabilityQuery(dbContext);
        var result = (await sut.GetCapabilities()).ToDictionary(x => x.Id);

        Assert.Single(result[compliantCapability.Id].Members);
        Assert.Equal(compliantMember.Id.ToString(), result[compliantCapability.Id].Members[0].UserId);

        Assert.Empty(result[nonCompliantCapability.Id].Members);
    }
}

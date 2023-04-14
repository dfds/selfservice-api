using SelfService.Domain.Models;
using SelfService.Tests.Comparers;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestPostgresMappings
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task capability()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.Capability.Build();

        // write
        await dbContext.Capabilities.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.Capabilities.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new CapabilityComparer()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task member()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.Member.Build();

        // write
        await dbContext.Members.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.Members.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new MemberComparer()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task membership()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.Membership.Build();

        // write
        await dbContext.Memberships.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.Memberships.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new MembershipComparer()
        );
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task awsaccount()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.AwsAccount.Build();
        stub.RegisterRealAwsAccount(RealAwsAccountId.Parse(new string('0', 12)), "foo@foo.com", new DateTime(2000, 1, 1));
        stub.LinkKubernetesNamespace("the-namespace", new DateTime(2000, 1, 1));

        // write
        await dbContext.AwsAccounts.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.AwsAccounts.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new AwsAccountComparer()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task kafkacluster()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.KafkaCluster.Build();

        // write
        await dbContext.KafkaClusters.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.KafkaClusters.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new KafkaClusterComparer()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task kafkatopic()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.KafkaTopic.Build();

        // write
        await dbContext.KafkaTopics.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.KafkaTopics.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new KafkaTopicComparer()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task membershipapplication()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stubApprovals = new[]
        {
            A.MembershipApproval.Build(),
            A.MembershipApproval.Build(),
        };
        
        var stub = A.MembershipApplication
            .WithApprovals(stubApprovals)
            .Build();

        // write
        await dbContext.MembershipApplications.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.MembershipApplications.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new MembershipApplicationComparer()
        );

        Assert.Equal(
            expected: stubApprovals,
            actual: storedVersion!.Approvals,
            comparer: new MembershipApprovalComparer()
        );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task messagecontract()
    {
        await using var databaseFactory = new ExternalDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.MessageContract.Build();
        
        // write
        await dbContext.MessageContracts.AddAsync(stub);
        await dbContext.SaveChangesAsync();

        // read
        var storedVersion = await dbContext.MessageContracts.FindAsync(stub.Id);

        Assert.Equal(
            expected: stub,
            actual: storedVersion,
            comparer: new MessageContractComparer()
        );
    }
}
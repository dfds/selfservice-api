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
}
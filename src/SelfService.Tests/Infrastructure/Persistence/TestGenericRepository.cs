using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Converters;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestGenericRepository
{
    private class TestGenericRepositoryModelId : ValueObjectGuid<TestGenericRepositoryModelId>
    {
        public TestGenericRepositoryModelId(Guid guid)
            : base(guid) { }
    }

    private class TestGenericRepositoryModel : Entity<TestGenericRepositoryModelId>
    {
        public string Foo { get; set; } = string.Empty;
        public int Bar { get; set; }

        public TestGenericRepositoryModel(TestGenericRepositoryModelId id, string foo, int bar)
            : base(id)
        {
            Foo = foo;
            Bar = bar;
        }
    }

    private class TestDbContext : SelfServiceDbContext
    {
        public DbSet<TestGenericRepositoryModel> GenericRepositoryModels => Set<TestGenericRepositoryModel>();

        public TestDbContext(DbContextOptions<SelfServiceDbContext> options)
            : base(options) { }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder
                .Properties<TestGenericRepositoryModelId>()
                .HaveConversion<ValueObjectGuidConverter<TestGenericRepositoryModelId>>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestGenericRepositoryModel>(cfg =>
            {
                cfg.ToTable("TestGenericRepositoryModels");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.Id).HasColumnName("Id");
                cfg.Property(x => x.Foo).IsRequired();
                cfg.Property(x => x.Bar).IsRequired();
            });
        }
    }

    private static readonly TestGenericRepositoryModelId TestId = TestGenericRepositoryModelId.New();

    private async Task<
        GenericRepository<TestGenericRepositoryModel, TestGenericRepositoryModelId>
    > CreateGenericRepoAndAddOne(TestDbContext dbContext)
    {
        var repo = new GenericRepository<TestGenericRepositoryModel, TestGenericRepositoryModelId>(
            dbContext.GenericRepositoryModels
        );

        await repo.Add(new TestGenericRepositoryModel(TestId, "foo", 1));
        await dbContext.SaveChangesAsync();
        return repo;
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_add_to_repo()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = await CreateGenericRepoAndAddOne(dbContext);

        var allObjects = await repo.GetAll();

        Assert.Single(allObjects);
        Assert.Equal(TestId, allObjects[0].Id);
        Assert.Equal("foo", allObjects[0].Foo);
        Assert.Equal(1, allObjects[0].Bar);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_delete_from_repo()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = await CreateGenericRepoAndAddOne(dbContext);

        // sanity check
        var allObjects = await repo.GetAll();
        Assert.Single(allObjects);

        await repo.Remove(TestId);
        await dbContext.SaveChangesAsync();
        var allObjectsAfterDeletion = await repo.GetAll();
        Assert.Empty(allObjectsAfterDeletion);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_find_by_id()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = await CreateGenericRepoAndAddOne(dbContext);

        var otherId = TestGenericRepositoryModelId.New();
        await repo.Add(new TestGenericRepositoryModel(otherId, "woo", 10));
        await dbContext.SaveChangesAsync();
        var toBeFound = await repo.FindById(TestId);
        Assert.NotNull(toBeFound);
        var toNotBeFound = await repo.FindById(TestGenericRepositoryModelId.New());
        Assert.Null(toNotBeFound);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_find_with_exists()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = await CreateGenericRepoAndAddOne(dbContext);
        var doesExist = await repo.Exists(TestId);
        Assert.True(doesExist);
        var doesNotExist = await repo.Exists(TestGenericRepositoryModelId.New());
        Assert.False(doesNotExist);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task using_deal_db_set_works()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<SelfServiceDbContext>(
            options => new TestDbContext(options)
        );

        var repo = new GenericRepository<KafkaTopic, KafkaTopicId>(dbContext.KafkaTopics);
        var kafkaTopic = A.KafkaTopic.Build();
        await repo.Add(kafkaTopic);
        await dbContext.SaveChangesAsync();

        var allObjects = await repo.GetAll();

        Assert.Single(allObjects);
        Assert.Equal(kafkaTopic.Id, allObjects[0].Id);
        Assert.Equal(kafkaTopic.Name, allObjects[0].Name);
        Assert.Equal(kafkaTopic.Status, allObjects[0].Status);
        Assert.Equal(kafkaTopic.CreatedAt, allObjects[0].CreatedAt);
        Assert.Equal(kafkaTopic.CreatedBy, allObjects[0].CreatedBy);
        Assert.Equal(kafkaTopic.KafkaClusterId, allObjects[0].KafkaClusterId);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_find_with_predicate()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = await CreateGenericRepoAndAddOne(dbContext);
        var doesExist = await repo.FindByPredicate(x => x.Foo == "foo");
        Assert.NotNull(doesExist);
        var doesNotExist = await repo.FindByPredicate(x => x.Foo == "bar");
        Assert.Null(doesNotExist);
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task can_get_all_with_predicate()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = await CreateGenericRepoAndAddOne(dbContext);

        await repo.Add(new TestGenericRepositoryModel(TestGenericRepositoryModelId.New(), "foo", 2));
        await repo.Add(new TestGenericRepositoryModel(TestGenericRepositoryModelId.New(), "foo", 3));
        await repo.Add(new TestGenericRepositoryModel(TestGenericRepositoryModelId.New(), "bar", 1));
        await dbContext.SaveChangesAsync();

        var allObjects2 = await repo.GetAllWithPredicate(x => x.Foo == "foo");
        Assert.Equal(3, allObjects2.Count);

        var allObjects3 = await repo.GetAllWithPredicate(x => x.Foo == "bar");
        Assert.Single(allObjects3);
    }
}

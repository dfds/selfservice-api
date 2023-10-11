using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Converters;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestGenericRepository
{
    private class TestGenericRepositoryModelId : ValueObjectGuid<TestGenericRepositoryModelId>
    {
        public TestGenericRepositoryModelId()
            : base(new Guid()) { }
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
        public DbSet<TestGenericRepositoryModel> TestGenericRepositoryModels { get; set; } = null!;

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

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_inserts_correctly()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext<TestDbContext>(options => new TestDbContext(options));

        var repo = new GenericRepository<TestGenericRepositoryModel, TestGenericRepositoryModelId>(
            dbContext.TestGenericRepositoryModels
        );

        var id = new TestGenericRepositoryModelId();
        await repo.Add(new TestGenericRepositoryModel(id, "foo", 1));
        await dbContext.SaveChangesAsync();

        var allObjects = await repo.GetAll();

        Assert.Single(allObjects);
        Assert.Equal(id, allObjects[0].Id);
        Assert.Equal("foo", allObjects[0].Foo);
        Assert.Equal(1, allObjects[0].Bar);
    }
}

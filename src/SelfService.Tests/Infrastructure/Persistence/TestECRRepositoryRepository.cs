namespace SelfService.Tests.Infrastructure.Persistence;

public class TestECRRepositoryRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_inserts_expected_ecr_repository_into_database()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.ECRRepository;

        var sut = A.ECRRepositoryRepository.WithDbContext(dbContext).Build();

        await sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.ECRRepositories.ToListAsync());
        Assert.Equal(stub, inserted, new ECRRepositoryComparer());
    }
}

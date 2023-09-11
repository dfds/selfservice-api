using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestECRRepositoryRepository
{
    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_inserts_expected_ecr_repository_into_database()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var stub = A.ECRRepository.Build();

        var sut = A.ECRRepositoryRepository.WithDbContext(dbContext).Build();

        sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.ECRRepositories.ToListAsync());
        Assert.True(ECRRepositoriesAreEqual(stub, inserted));
    }

    private bool ECRRepositoriesAreEqual(ECRRepository mine, ECRRepository theirs)
    {
        return mine.Id == theirs.Id
            && mine.Name == theirs.Name
            && mine.Description == theirs.Description
            && mine.RepositoryName == theirs.RepositoryName
            && mine.CreatedBy == theirs.CreatedBy;
    }
}

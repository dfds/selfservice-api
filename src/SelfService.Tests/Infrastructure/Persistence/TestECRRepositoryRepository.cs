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
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stub = A.ECRRepository.Build();

        var sut = A.ECRRepositoryRepository.WithDbContext(dbContext).Build();

        await sut.Add(stub);

        await dbContext.SaveChangesAsync();

        var inserted = Assert.Single(await dbContext.ECRRepositories.ToListAsync());
        Assert.True(ECRRepositoriesAreEqual(stub, inserted));
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task add_range_inserts_expected_ecr_repositories_into_database()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var stubs = new List<ECRRepository>()
        {
            A.ECRRepository.WithName("ecr/first-stub").Build(),
            A.ECRRepository.WithName("ecr/second-stub").Build(),
        };
        var sut = A.ECRRepositoryRepository.WithDbContext(dbContext).Build();

        await sut.AddRange(stubs);

        await dbContext.SaveChangesAsync();

        var inserted = await dbContext.ECRRepositories.ToListAsync();
        inserted.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        Assert.Equal(2, inserted.Count);
        Assert.True(ECRRepositoriesAreEqual(stubs[0], inserted[0]));
        Assert.True(ECRRepositoriesAreEqual(stubs[1], inserted[1]));
    }

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task remove_with_repository_name_removes_expected_ecr_repository_from_database()
    {
        const string toBeDeletedRepositoryName = "to-be-deleted";
        const string notToBeDeletedRepositoryName = "not-to-be-deleted";

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateSelfServiceDbContext();

        var repositoryToBeDeleted = A.ECRRepository.WithName(toBeDeletedRepositoryName).Build();
        var repositoryToNotBeDeleted = A.ECRRepository.WithName(notToBeDeletedRepositoryName).Build();

        var sut = A.ECRRepositoryRepository.WithDbContext(dbContext).Build();

        await sut.Add(repositoryToBeDeleted);
        await sut.Add(repositoryToNotBeDeleted);
        await dbContext.SaveChangesAsync();

        sut.RemoveRangeWithRepositoryName(new List<string> { toBeDeletedRepositoryName });
        await dbContext.SaveChangesAsync();

        var repositories = await dbContext.ECRRepositories.ToListAsync();
        Assert.Single(repositories);
        Assert.Equal(notToBeDeletedRepositoryName, repositories[0].Name);
    }

    private bool ECRRepositoriesAreEqual(ECRRepository mine, ECRRepository theirs)
    {
        return mine.Id == theirs.Id
            && mine.Name == theirs.Name
            && mine.Description == theirs.Description
            && mine.CreatedBy == theirs.CreatedBy;
    }
}

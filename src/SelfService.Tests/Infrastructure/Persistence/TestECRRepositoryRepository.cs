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

    [Fact]
    [Trait("Category", "InMemoryDatabase")]
    public async Task remove_with_repository_name_removes_expected_ecr_repository_from_database()
    {
        const string toBeDeletedRepositoryName = "to be deleted";
        const string notToBeDeletedRepositoryName = "not to be deleted";

        await using var databaseFactory = new InMemoryDatabaseFactory();
        var dbContext = await databaseFactory.CreateDbContext();

        var repositoryToBeDeleted = A.ECRRepository.WithRepositoryName(toBeDeletedRepositoryName).Build();
        var repositoryToNotBeDeleted = A.ECRRepository.WithRepositoryName(notToBeDeletedRepositoryName).Build();

        var sut = A.ECRRepositoryRepository.WithDbContext(dbContext).Build();

        await sut.Add(repositoryToBeDeleted);
        await sut.Add(repositoryToNotBeDeleted);
        await dbContext.SaveChangesAsync();

        await sut.RemoveWithRepositoryName(toBeDeletedRepositoryName);

        var repositories = await dbContext.ECRRepositories.ToListAsync();
        Assert.Single(repositories);
        Assert.Equal(notToBeDeletedRepositoryName, repositories[0].RepositoryName);
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

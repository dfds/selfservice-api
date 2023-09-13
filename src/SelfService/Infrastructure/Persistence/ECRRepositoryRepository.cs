using Microsoft.EntityFrameworkCore;
using SelfService.Domain;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class ECRRepositoryRepository : IECRRepositoryRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public ECRRepositoryRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ECRRepository>> GetAll()
    {
        return await _dbContext.ECRRepositories.ToListAsync();
    }

    [TransactionalBoundary]
    public async Task Add(ECRRepository ecrRepository)
    {
        await _dbContext.ECRRepositories.AddAsync(ecrRepository);
    }

    [TransactionalBoundary]
    public async Task AddRange(List<ECRRepository> ecrRepositories)
    {
        await _dbContext.AddRangeAsync(ecrRepositories);
    }

    [TransactionalBoundary]
    public void RemoveRangeWithRepositoryName(List<string> repositoryNames)
    {
        var repositories = _dbContext.ECRRepositories.Where(x => repositoryNames.Contains(x.RepositoryName));
        _dbContext.ECRRepositories.RemoveRange(repositories);
    }
}

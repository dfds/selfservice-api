using Microsoft.EntityFrameworkCore;
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

    public void Add(ECRRepository ecr)
    {
        _dbContext.ECRRepositories.Add(ecr);
    }

    public Task RemoveWithRepositoryName(string repositoryName)
    {
        var repo = _dbContext.ECRRepositories.Single(x => x.RepositoryName == repositoryName);
        _dbContext.Remove(repo);
        return Task.CompletedTask;
    }
}

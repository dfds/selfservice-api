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

    public async Task Add(ECRRepository ecrRepository)
    {
        await _dbContext.ECRRepositories.AddAsync(ecrRepository);
    }

    public async Task AddRange(List<ECRRepository> ecrRepositories)
    {
        await _dbContext.ECRRepositories.AddRangeAsync(ecrRepositories);
    }

    public void RemoveRangeWithRepositoryName(List<string> repositoryNames)
    {
        var repositories = _dbContext.ECRRepositories.Where(x => repositoryNames.Contains(x.RepositoryName));
        _dbContext.ECRRepositories.RemoveRange(repositories);
    }
}

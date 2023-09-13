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

    public async Task Add(ECRRepository ecrRepository)
    {
        await _dbContext.ECRRepositories.AddAsync(ecrRepository);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRange(List<ECRRepository> ecrRepositories)
    {
        await _dbContext.AddRangeAsync(ecrRepositories);
        await _dbContext.SaveChangesAsync();
    }

    public Task RemoveWithRepositoryName(string repositoryName)
    {
        var repo = _dbContext.ECRRepositories.Single(x => x.RepositoryName == repositoryName);
        _dbContext.ECRRepositories.Remove(repo);
        return _dbContext.SaveChangesAsync();
    }
}

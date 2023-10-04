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

    public Task<bool> HasRepository(string Name)
    {
        return _dbContext.ECRRepositories.AnyAsync(x => x.Name == Name);
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

    [TransactionalBoundary]
    public void RemoveRangeWithRepositoryName(List<string> names)
    {
        var repositories = _dbContext.ECRRepositories.Where(x => names.Contains(x.Name));
        _dbContext.ECRRepositories.RemoveRange(repositories);
    }
}

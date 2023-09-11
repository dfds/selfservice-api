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

    public IEnumerable<ECRRepository> GetAll()
    {
        return _dbContext.ECRRepositories.ToList();
    }

    public void Add(ECRRepository ecr)
    {
        _dbContext.ECRRepositories.Add(ecr);
    }
}

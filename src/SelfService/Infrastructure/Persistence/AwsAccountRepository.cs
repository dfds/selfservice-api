using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class AwsAccountRepository : IAwsAccountRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public AwsAccountRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AwsAccount?> FindBy(CapabilityId capabilityId)
    {
        var accounts = await _dbContext
            .AwsAccounts.Where(x => x.CapabilityId == capabilityId)
            .OrderByDescending(x => x.Environment == "prod")
            .ThenBy(x => x.RequestedAt)
            .ToListAsync();

        return accounts.FirstOrDefault();
    }

    public Task<AwsAccount?> FindBy(CapabilityId capabilityId, string environment)
    {
        return _dbContext.AwsAccounts.SingleOrDefaultAsync(x =>
            x.CapabilityId == capabilityId && x.Environment == environment
        );
    }

    public Task<AwsAccount?> FindBy(AwsAccountId id)
    {
        return _dbContext.AwsAccounts.SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<AwsAccount>> GetAllBy(CapabilityId capabilityId)
    {
        return _dbContext.AwsAccounts.Where(x => x.CapabilityId == capabilityId).ToListAsync();
    }

    public async Task<List<AwsAccount>> GetAll()
    {
        return await _dbContext.AwsAccounts.ToListAsync();
    }

    public async Task<List<AwsAccount>> GetByCapabilityIds(IEnumerable<CapabilityId> capabilityIds)
    {
        var idList = capabilityIds.ToList();
        if (idList.Count == 0)
        {
            return new List<AwsAccount>();
        }
        return await _dbContext.AwsAccounts.Where(a => idList.Contains(a.CapabilityId)).ToListAsync();
    }

    public async Task<AwsAccount> Get(AwsAccountId id)
    {
        var found = await _dbContext.AwsAccounts.FindAsync(id);
        if (found is null)
        {
            throw EntityNotFoundException<AwsAccount>.UsingId(id);
        }

        return found;
    }

    public async Task Add(AwsAccount account)
    {
        await _dbContext.AwsAccounts.AddAsync(account);
    }

    public async Task<bool> Exists(CapabilityId capabilityId)
    {
        return await _dbContext.AwsAccounts.AnyAsync(x => x.CapabilityId == capabilityId);
    }

    public async Task<bool> Exists(CapabilityId capabilityId, string environment)
    {
        return await _dbContext.AwsAccounts.AnyAsync(x =>
            x.CapabilityId == capabilityId && x.Environment == environment
        );
    }

    public async Task<int> CountBy(CapabilityId capabilityId)
    {
        return await _dbContext.AwsAccounts.CountAsync(x => x.CapabilityId == capabilityId);
    }
}

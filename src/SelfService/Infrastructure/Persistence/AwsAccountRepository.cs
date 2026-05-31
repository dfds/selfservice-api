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

    public Task<AwsAccount?> FindBy(CapabilityId capabilityId)
    {
        return _dbContext.AwsAccounts.SingleOrDefaultAsync(x => x.CapabilityId == capabilityId);
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
}

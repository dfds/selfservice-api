using Microsoft.EntityFrameworkCore;
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
        return await _dbContext
            .AwsAccounts
            .ToListAsync();
    }
}
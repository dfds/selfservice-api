using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class KubernetesAccessRepository : IKubernetesAccessRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public KubernetesAccessRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(KubernetesAccess access)
    {
        await _dbContext.KubernetesAccesses.AddAsync(access);
    }

    public Task<List<KubernetesAccess>> GetAllBy(CapabilityId capabilityId)
    {
        return _dbContext
            .KubernetesAccesses.Where(x => x.CapabilityId == capabilityId)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync();
    }

    public Task<List<KubernetesAccess>> GetAllBy(IEnumerable<CapabilityId> capabilityIds)
    {
        var ids = capabilityIds.ToList();
        return _dbContext.KubernetesAccesses.Where(x => ids.Contains(x.CapabilityId)).ToListAsync();
    }

    // Returns the most recent requested (not yet granted) access for the given AWS account
    public Task<KubernetesAccess?> FindRequestedByAwsAccountId(AwsAccountId awsAccountId)
    {
        return _dbContext
            .KubernetesAccesses.Where(x => x.AwsAccountId == awsAccountId && x.GrantedAt == null)
            .OrderByDescending(x => x.RequestedAt)
            .FirstOrDefaultAsync();
    }
}

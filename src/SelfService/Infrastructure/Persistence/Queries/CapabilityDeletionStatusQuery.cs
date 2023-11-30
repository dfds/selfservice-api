using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class CapabilityDeletionStatusQuery : ICapabilityDeletionStatusQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public CapabilityDeletionStatusQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsPendingDeletion(CapabilityId capabilityId)
    {
        var capability = await _dbContext.Capabilities.FindAsync(capabilityId);
        return capability?.Status == CapabilityStatusOptions.PendingDeletion;
    }
}

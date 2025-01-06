using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class AzureResourceRepository : IAzureResourceRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public AzureResourceRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AzureResource>> GetAll()
    {
        return await _dbContext.AzureResources.ToListAsync();
    }

    public async Task<List<AzureResource>> GetFor(CapabilityId capabilityId)
    {
        return await _dbContext.AzureResources.Where(x => x.CapabilityId == capabilityId).ToListAsync();
    }

    public async Task<AzureResource> Get(AzureResourceId id)
    {
        var found = await _dbContext.AzureResources.FindAsync(id);
        if (found is null)
        {
            throw EntityNotFoundException<AzureResource>.UsingId(id);
        }

        return found;
    }

    public async Task Add(AzureResource resource)
    {
        await _dbContext.AzureResources.AddAsync(resource);
    }

    public async Task<bool> Exists(CapabilityId capabilityId, string environment)
    {
        return await _dbContext.AzureResources.AnyAsync(x =>
            x.CapabilityId == capabilityId && x.Environment == environment
        );
    }

    public async Task<bool> Any(CapabilityId capabilityId)
    {
        return await _dbContext.AzureResources.AnyAsync(x => x.CapabilityId == capabilityId);
    }
}

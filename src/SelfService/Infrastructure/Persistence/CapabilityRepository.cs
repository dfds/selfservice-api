using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class CapabilityRepository : ICapabilityRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public CapabilityRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Capability> Get(CapabilityId id)
    {
        var found = await _dbContext.Capabilities.FindAsync(id);
        if (found is null)
        {
            throw EntityNotFoundException<Capability>.UsingId(id);
        }

        return found;
    }

    public async Task<Capability?> FindBy(CapabilityId id)
    {
        return await _dbContext.Capabilities.FindAsync(id);
    }

    public async Task<bool> Exists(CapabilityId id)
    {
        return await _dbContext.Capabilities.AnyAsync(x => x.Id == id);
    }

    public async Task Add(Capability capability)
    {
        await _dbContext.Capabilities.AddAsync(capability);
    }

    public async Task<IEnumerable<Capability>> GetAll()
    {
        return await _dbContext.Capabilities
            .Where(c => c.Status != CapabilityStatusOptions.Deleted)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Capability>> GetAllPendingDeletionFor(int days)
    {
        var targetDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(days));
        return await _dbContext.Capabilities
            .Where(c => c.Status == CapabilityStatusOptions.PendingDeletion && c.ModifiedAt <= targetDate)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}

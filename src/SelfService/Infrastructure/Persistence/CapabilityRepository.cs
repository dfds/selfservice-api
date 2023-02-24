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

    public async Task<Capability> GetById(CapabilityId id)
    {
        var found = await _dbContext
            .Capabilities
            .SingleOrDefaultAsync(x => x.Id == id);

        if (found is null)
        {
            throw new EntityNotFoundException($"Capability with id \"{id}\" could not be found.");
        }

        return found;
    }

    public async Task<bool> Exists(CapabilityId id)
    {
        return await _dbContext.Capabilities.AnyAsync(x => x.Id == id);
    }

    public async Task Add(Capability capability)
    {
        await _dbContext.Capabilities.AddAsync(capability);
    }
}
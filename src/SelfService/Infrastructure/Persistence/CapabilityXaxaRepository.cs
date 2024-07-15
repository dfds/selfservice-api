using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class CapabilityXaxaRepository : ICapabilityXaxaRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public CapabilityXaxaRepository(SelfServiceDbContext serviceDbContext)
    {
        _dbContext = serviceDbContext;
    }
    
    public async Task Add(CapabilityXaxa capabilityXaxa)
    {
        await _dbContext.CapabilityXaxa.AddAsync(capabilityXaxa);
        await _dbContext.SaveChangesAsync();
    }
}
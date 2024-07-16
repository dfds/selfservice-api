using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Queries;

public class CapabilityClaimRepository : ICapabilityCalimRepository
{
    private readonly SelfServiceDbContext _dbContext;
    
    public CapabilityClaimRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task Add(CapabilityClaim claim)
    {
        await _dbContext.CapabilityClaims.AddAsync(claim);
        await _dbContext.SaveChangesAsync();
    }
}
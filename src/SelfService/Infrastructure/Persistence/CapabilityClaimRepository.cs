using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence.Queries;

public class CapabilityClaimRepository : ICapabilityClaimRepository
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

    public async Task<bool> CheckClaim(CapabilityId capabilityId, string claimType)
    {
        return await _dbContext.CapabilityClaims.AnyAsync(c => c.CapabilityId == capabilityId && c.Claim == claimType);
    }

    public async Task<List<CapabilityClaim>> GetAll(CapabilityId capabilityId)
    {
        return await _dbContext.CapabilityClaims.Where(x => x.CapabilityId == capabilityId).ToListAsync();
    }
}

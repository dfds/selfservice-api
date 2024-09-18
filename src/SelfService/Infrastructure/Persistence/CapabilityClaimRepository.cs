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

    public async Task<bool> ClaimExists(CapabilityId capabilityId, string claimType)
    {
        return await _dbContext.CapabilityClaims.AnyAsync(c => c.CapabilityId == capabilityId && c.Claim == claimType);
    }

    public async Task<List<CapabilityClaim>> GetAll(CapabilityId capabilityId)
    {
        return await _dbContext.CapabilityClaims.Where(x => x.CapabilityId == capabilityId).ToListAsync();
    }

    public async Task<CapabilityClaim?> Get(CapabilityId capabilityId, string claimType)
    {
        return await _dbContext.CapabilityClaims.FirstOrDefaultAsync(
            c => c.CapabilityId == capabilityId && c.Claim == claimType
        );
    }

    public async Task Remove(CapabilityClaim claim)
    {
        _dbContext.CapabilityClaims.Remove(claim);
        await _dbContext.SaveChangesAsync();
    }

    /*
     * [2024-07-22] andfris: Temporary solution
     * The following claims should be stored in a database rather than in code.
     * This is a temporary solution to get the feature up and running quickly.
     * If the feature is to be kept, the claims should be moved to a database.
     */
    public List<CapabilityClaimOption> ListPossibleClaims()
    {
        return new List<CapabilityClaimOption>
        {
            new CapabilityClaimOption(claimType: "snyk", claimDescription: "Code is monitored by Snyk"),
        };
    }
}

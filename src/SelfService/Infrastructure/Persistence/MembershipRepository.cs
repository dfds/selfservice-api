using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MembershipRepository : IMembershipRepository
{
    private readonly SelfServiceDbContext _dbContext;

    public MembershipRepository(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Add(Membership membership)
    {
        await _dbContext.Memberships.AddAsync(membership);
    }

    public async Task<IEnumerable<Membership>> FindBy(CapabilityId capabilityId)
    {
        return await _dbContext.Memberships
            .Where(x => x.CapabilityId == capabilityId)
            .ToListAsync();
    }
}
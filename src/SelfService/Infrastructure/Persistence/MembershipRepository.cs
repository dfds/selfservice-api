using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Exceptions;

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
        return await _dbContext.Memberships.Where(x => x.CapabilityId == capabilityId).ToListAsync();
    }

    public async Task<bool> IsAlreadyMember(CapabilityId capabilityId, UserId userId)
    {
        var count = await _dbContext.Memberships.Where(x => x.CapabilityId == capabilityId && x.UserId == userId).CountAsync();
        return (count > 0);
    }

    public async Task<Membership?> CancelWithCapabilityId(CapabilityId capabilityId, UserId userId)
    {
        var membershipCount = await _dbContext.Memberships.Where(x => x.CapabilityId == capabilityId).CountAsync();
        if (membershipCount <= 1)
        {
            return null;
        }

        var membership = await _dbContext.Memberships
            .Where(x => x.CapabilityId == capabilityId && x.UserId == userId)
            .FirstOrDefaultAsync();
        if (membership == null)
        {
            throw new EntityNotFoundException<Membership>(
                $"No Membership for user \"{userId}\" in capability \"{capabilityId}\""
            );
        }

        _dbContext.Memberships.Remove(membership);

        return membership;
    }

    public async Task<List<Membership>> CancelAllMembershipsWithUserId(UserId userId)
    {
        var memberships = await _dbContext.Memberships.Where(x => x.UserId == userId).ToListAsync();

        foreach (var membership in memberships)
        {
            _dbContext.Memberships.Remove(membership);
        }

        return memberships;
    }
}

using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class MembershipRepository : GenericRepository<Membership, MembershipId>, IMembershipRepository
{
    public MembershipRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Memberships) { }

    public async Task<IEnumerable<Membership>> FindBy(CapabilityId capabilityId)
    {
        return await GetAllWithPredicate(x => x.CapabilityId == capabilityId);
    }

    public async Task<bool> IsAlreadyMember(CapabilityId capabilityId, UserId userId)
    {
        var memberships = await GetAllWithPredicate(x => x.CapabilityId == capabilityId && x.UserId == userId);
        return memberships.Count > 0;
    }

    public async Task<Membership?> CancelWithCapabilityId(CapabilityId capabilityId, UserId userId)
    {
        var memberships = await GetAllWithPredicate(x => x.CapabilityId == capabilityId);
        if (memberships.Count <= 1)
        {
            return null;
        }

        var membership = await FindByPredicate(x => x.CapabilityId == capabilityId && x.UserId == userId);
        if (membership == null)
        {
            throw new EntityNotFoundException<Membership>(
                $"No Membership for user \"{userId}\" in capability \"{capabilityId}\""
            );
        }

        await Remove(membership.Id);

        return membership;
    }

    public async Task<List<Membership>> CancelAllMembershipsWithUserId(UserId userId)
    {
        var memberships = await GetAllWithPredicate(x => x.UserId == userId);

        foreach (var membership in memberships)
        {
            await Remove(membership.Id);
        }

        return memberships;
    }

    public async Task<List<Membership>> GetAllMembershipsForUserId(UserId userId)
    {
        return await GetAllWithPredicate(x => x.UserId == userId);
    }

    public async Task<List<Membership>> GetAllMembershipsForUserIds(IEnumerable<UserId> userIds)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new List<Membership>();
        }
        return await DbSetReference.Where(x => idList.Contains(x.UserId)).ToListAsync();
    }

    public async Task<Dictionary<CapabilityId, int>> GetMemberCountsByCapabilityIds(
        IEnumerable<CapabilityId> capabilityIds
    )
    {
        var idList = capabilityIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<CapabilityId, int>();
        }
        // Project to the capability id column only, then group in memory — avoids relying on
        // EF Core translating GroupBy over a value-converted key.
        var capIds = await DbSetReference
            .Where(x => idList.Contains(x.CapabilityId))
            .Select(x => x.CapabilityId)
            .ToListAsync();
        return capIds.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
    }
}

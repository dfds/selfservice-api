using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Persistence.Queries;

public class CapabilityMembersQuery : ICapabilityMembersQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public CapabilityMembersQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Member>> FindBy(CapabilityId capabilityId)
    {
        var query = from membership in _dbContext.Memberships
            join member in _dbContext.Members on membership.UserId equals member.Id
            where membership.CapabilityId == capabilityId
            select member;

        return await query
            .OrderBy(x => x.Id)
            .ToListAsync();
    }
}

public class MyCapabilitiesQuery : IMyCapabilitiesQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public MyCapabilitiesQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Capability>> FindBy(UserId userId)
    {
        var query = from membership in _dbContext.Memberships
            join capability in _dbContext.Capabilities on membership.CapabilityId equals capability.Id
            where membership.UserId == userId
            select capability;

        return await query
            .OrderBy(x => x.Name)
            .Where(x => x.Deleted == null)
            .ToListAsync();
    }
}

public class CapabilityKafkaTopicsQuery : ICapabilityKafkaTopicsQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public CapabilityKafkaTopicsQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<KafkaTopic>> FindBy(CapabilityId capabilityId)
    {
        return await _dbContext.KafkaTopics
            .Where(x => x.CapabilityId == capabilityId)
            .OrderBy(x => x.Name)
            .ToListAsync();
    }
}

public class MembershipQuery :  IMembershipQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public MembershipQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasActiveMembership(UserId userId, CapabilityId capabilityId)
    {
        // NOTE [jandr@2023-03-16]: this is based on membership entities are deleted when 
        // members leave a capability - but it would be better if the membership became
        // inactive instead (maybe with a termination date on the entity)

        var activeMemberships = await _dbContext.Memberships
            .Where(x => x.UserId == userId && x.CapabilityId == capabilityId)
            .CountAsync();

        return activeMemberships > 0;
    }

    public async Task<bool> HasActiveMembershipApplication(UserId userId, CapabilityId capabilityId)
    {
        var activeMembershipApplications = await _dbContext.MembershipApplications
            .Where(x => x.Applicant == userId && x.CapabilityId == capabilityId && x.Status == MembershipApplicationStatusOptions.PendingApprovals)
            .CountAsync();

        return activeMembershipApplications > 0;
    }

    public async Task<bool> HasMultipleMembers(CapabilityId capabilityId)
    {
        var members = await _dbContext.Memberships
            .Where(x => x.CapabilityId == capabilityId)
            .CountAsync();

        return members > 1;
    }
}

public class CachedMembershipQueryDecorator : IMembershipQuery
{
    private readonly IMembershipQuery _inner;
    private readonly Dictionary<string, bool> _cache = new();

    public CachedMembershipQueryDecorator(IMembershipQuery inner)
    {
        _inner = inner;
    }

    public async Task<bool> HasActiveMembership(UserId userId, CapabilityId capabilityId)
    {
        var key = $"HAM:{userId}:{capabilityId}";
        
        if (_cache.TryGetValue(key, out var result))
        {
            return result;
        }

        var innerResult = await _inner.HasActiveMembership(userId, capabilityId);
        _cache.Add(key, innerResult);

        return innerResult;
    }

    public async Task<bool> HasActiveMembershipApplication(UserId userId, CapabilityId capabilityId)
    {
        var key = $"HAMA:{userId}:{capabilityId}";

        if (_cache.TryGetValue(key, out var result))
        {
            return result;
        }

        var innerResult = await _inner.HasActiveMembershipApplication(userId, capabilityId);
        _cache.Add(key, innerResult);

        return innerResult;
    }
}


public class CapabilityMembershipApplicationQuery : ICapabilityMembershipApplicationQuery
{
    private readonly SelfServiceDbContext _dbContext;

    public CapabilityMembershipApplicationQuery(SelfServiceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<MembershipApplication>> FindPendingBy(CapabilityId capabilityId)
    {
        return await _dbContext.MembershipApplications
            .Include(x => x.Approvals)
            .Where(x => x.CapabilityId == capabilityId && x.Status == MembershipApplicationStatusOptions.PendingApprovals)
            .OrderBy(x => x.SubmittedAt)
            .ToListAsync();
    }
}

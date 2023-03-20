﻿using Microsoft.EntityFrameworkCore;
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
        // members leave a capability - but it would be better of the membership became
        // inactive instead (maybe with a termination date on the entity)

        var activeMemberships = await _dbContext.Memberships
            .Where(x => x.UserId == userId && x.CapabilityId == capabilityId)
            .CountAsync();

        return activeMemberships > 0;
    }

    public async Task<IEnumerable<Membership>> FindActiveBy(UserId userId)
    {
        // NOTE [jandr@2023-03-16]: this is based on membership entities are deleted when 
        // members leave a capability - but it would be better of the membership became
        // inactive instead (maybe with a termination date on the entity)

        return await _dbContext.Memberships
            .Where(x => x.UserId == userId)
            .ToListAsync();
    }
}

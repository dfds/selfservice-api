using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.System;

namespace SelfService.Infrastructure.Persistence.Queries;

public class AadAwsSyncCapabilityQuery : IAadAwsSyncCapabilityQuery
{
    private readonly SelfServiceDbContext _context;

    public AadAwsSyncCapabilityQuery(SelfServiceDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CapabilityDto>> GetCapabilities()
    {
        var allCapabilities = await GetAllActiveCapabilities();
        var allMemberships = await GetAllMembershipByCapability();
        var allAwsAccounts = await GetAllAwsAccountsByCapability();

        return from capability in allCapabilities
            let memberships = allMemberships[capability.Id]
            let awsAccounts = allAwsAccounts[capability.Id]
            select new CapabilityDto
            {
                Id = capability.Id,
                Name = capability.Name,
                RootId = capability.Id,
                Description = capability.Description,
                Members = memberships
                    .Select<Membership, MemberDto>(member => new MemberDto { Email = member.UserId })
                    .ToArray(),
                Contexts = awsAccounts
                    .Select<AwsAccount, ContextDto>(
                        context =>
                            new ContextDto
                            {
                                Id = context.Id,
                                Name = "default",
                                AWSRoleArn = "",
                                AWSAccountId = context.Registration.AccountId?.ToString(),
                                AWSRoleEmail = context.Registration.RoleEmail
                            }
                    )
                    .ToArray(),
            };
    }

    private async Task<List<Capability>> GetAllActiveCapabilities()
    {
        return await _context.Capabilities.Where(x => x.Deleted == null).ToListAsync();
    }

    private async Task<ILookup<CapabilityId, Membership>> GetAllMembershipByCapability()
    {
        var memberships = await _context.Memberships.ToListAsync();
        return memberships.ToLookup(x => x.CapabilityId);
    }

    private async Task<ILookup<CapabilityId, AwsAccount>> GetAllAwsAccountsByCapability()
    {
        var awsAccounts = await _context.AwsAccounts.ToListAsync();
        return awsAccounts.ToLookup(x => x.CapabilityId);
    }
}

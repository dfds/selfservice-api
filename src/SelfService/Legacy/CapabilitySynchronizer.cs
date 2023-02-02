using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Legacy;

public class CapabilitySynchronizer
{
    private readonly ILogger<CapabilitySynchronizer> _logger;
    private readonly LegacyDbContext _legacyDbContext;
    private readonly SelfServiceDbContext _selfServiceDbContext;

    public CapabilitySynchronizer(ILogger<CapabilitySynchronizer> logger, LegacyDbContext legacyDbContext, SelfServiceDbContext selfServiceDbContext)
    {
        _logger = logger;
        _legacyDbContext = legacyDbContext;
        _selfServiceDbContext = selfServiceDbContext;
    }

    public async Task Synchronize(CancellationToken stoppingToken)
    {
        var legacyCapabilities = await GetAllLegacyCapabilities(stoppingToken);
        var capabilities = await GetAllCapabilities(stoppingToken);
        var members = await GetAllMembers(stoppingToken);
        var memberLookup = members.ToDictionary(x => x.UPN, StringComparer.InvariantCultureIgnoreCase);

        _logger.LogInformation("Legacy Capabilities {Count}", legacyCapabilities.Count);
        _logger.LogInformation("Capabilities {Count}", capabilities.Count);

        foreach (var legacyCapability in legacyCapabilities.Where(x => !string.IsNullOrEmpty(x.RootId)))
        {
            var capability = capabilities.FirstOrDefault(x => x.Id == legacyCapability.RootId);
            if (capability == null)
            {
                capability = new Capability
                {
                    Id = legacyCapability.RootId!,
                    Name = legacyCapability.Name!,
                    Description = legacyCapability.Description ?? "",
                    Deleted = legacyCapability.Deleted.HasValue ? DateTime.SpecifyKind(legacyCapability.Deleted.Value, DateTimeKind.Utc) : null
                };

                AddMemberships(legacyCapability, capability, memberLookup);
                AddAwsAccount(legacyCapability, capability);

                await _selfServiceDbContext.Capabilities.AddAsync(capability, stoppingToken);

                _logger.LogInformation("Adding capability {Id}", capability.Id);
            }
            else
            {
                capability.Name = legacyCapability.Name!;
                capability.Description = legacyCapability.Description ?? "";
                capability.Deleted = legacyCapability.Deleted;

                AddMemberships(legacyCapability, capability, memberLookup);
                AddAwsAccount(legacyCapability, capability);

                _logger.LogInformation("Updating capability {Id}", capability.Id);
            }
        }

        await _selfServiceDbContext.SaveChangesAsync(stoppingToken);
    }

    private Task<List<Models.Capability>> GetAllLegacyCapabilities(CancellationToken stoppingToken)
    {
        return _legacyDbContext.Capabilities
            .Include(x => x.Memberships)
            .Include(x => x.Contexts)
            .ToListAsync(stoppingToken);
    }

    private Task<List<Capability>> GetAllCapabilities(CancellationToken stoppingToken)
    {
        return _selfServiceDbContext.Capabilities
            .Include(x => x.Memberships)
            .Include(x => x.AwsAccount)
            .ToListAsync(stoppingToken);
    }

    private Task<List<Member>> GetAllMembers(CancellationToken stoppingToken)
    {
        return _selfServiceDbContext.Members.ToListAsync(stoppingToken);
    }

    private static void AddMemberships(Models.Capability legacyCapability, Capability capability, IDictionary<string, Member> memberLookup)
    {
        foreach (var legacyMembership in legacyCapability.Memberships)
        {
            var membership = capability.Memberships.FirstOrDefault(x => string.Equals(x.UPN, legacyMembership.Email, StringComparison.InvariantCultureIgnoreCase));
            if (membership != null)
            {
                // already has membership
                continue;
            }

            AddMembership(memberLookup, legacyMembership, capability);
        }
    }

    private static void AddMembership(IDictionary<string, Member> memberLookup, Legacy.Models.Membership legacyMembership, Capability capability)
    {

        Member member;
        if (memberLookup.ContainsKey(legacyMembership.Email))
        {
            member = memberLookup[legacyMembership.Email];
        }
        else
        {
            member = new Member
            {
                UPN = legacyMembership.Email,
                Email = legacyMembership.Email,
            };
            memberLookup.Add(member.UPN, member);
        }

        capability.Memberships.Add(new Membership
        {
            CapabilityId = capability.Id,
            Capability = capability,
            UPN = legacyMembership.Email,
            Member = member
        });
    }

    private static void AddAwsAccount(Models.Capability legacyCapability, Capability capability)
    {
        var context = legacyCapability.Contexts.SingleOrDefault();
        if (context == null)
        {
            return;
        }

        if (capability.AwsAccount == null)
        {
            var awsAccount = new AwsAccount
            {
                Id = context.Id,
                AccountId = context.AWSAccountId,
                RoleArn = context.AWSRoleArn,
                RoleEmail = context.AWSRoleEmail
            };
            capability.AwsAccount = awsAccount;
        }
        else
        {
            capability.AwsAccount.AccountId = context.AWSAccountId;
            capability.AwsAccount.RoleArn = context.AWSRoleArn;
            capability.AwsAccount.RoleEmail = context.AWSRoleEmail;
        }
    }
}
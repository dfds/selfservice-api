using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Legacy;

public class CapabilitySynchronizer
{
    private readonly ILogger<CapabilitySynchronizer> _logger;
    private readonly LegacyDbContext _legacyDbContext;
    private readonly SelfServiceDbContext _selfServiceDbContext;

    public CapabilitySynchronizer(ILogger<CapabilitySynchronizer> logger,
        LegacyDbContext legacyDbContext,
        SelfServiceDbContext selfServiceDbContext)
    {
        _logger = logger;
        _legacyDbContext = legacyDbContext;
        _selfServiceDbContext = selfServiceDbContext;
    }

    public async Task Synchronize(CancellationToken stoppingToken)
    {
        var legacyCapabilities = await GetAllLegacyCapabilities(stoppingToken);
        var capabilities = await GetAllCapabilities(stoppingToken);
        var existingMemberships = await GetAllMemberships(stoppingToken);
        var membershipLookup = existingMemberships.ToLookup(x => x.CapabilityId.ToString(), StringComparer.InvariantCultureIgnoreCase);
        var existingMembers = await GetAllMembers(stoppingToken);
        var memberLookup = existingMembers.ToDictionary(x => x.Id.ToString(), StringComparer.InvariantCultureIgnoreCase);

        _logger.LogInformation("Legacy Capabilities {Count}", legacyCapabilities.Count);
        _logger.LogInformation("Capabilities {Count}", capabilities.Count);

        foreach (var legacyCapability in legacyCapabilities)
        {
            // use root id as capability, using Id (Guid) as a fallback  
            var rootId = !string.IsNullOrEmpty(legacyCapability.RootId) ? legacyCapability.RootId : legacyCapability.Id.ToString();
            var capability = capabilities.FirstOrDefault(x => x.Id == rootId);
            if (capability == null)
            {
                // add new capability from legacy (v1)

                capability = new Capability(
                    id: CapabilityId.Parse(rootId),
                    name: legacyCapability.Name,
                    description: legacyCapability.Description ?? "",
                    deleted: legacyCapability.Deleted.HasValue ? DateTime.SpecifyKind(legacyCapability.Deleted.Value, DateTimeKind.Utc) : null,
                    createdAt: DateTime.UtcNow,
                    createdBy: "SYSTEM"
                );

                await _selfServiceDbContext.Capabilities.AddAsync(capability, stoppingToken);
                _logger.LogInformation("Adding capability {Id}", capability.Id);

                var awsAccount = ExtractAwsAccountFrom(legacyCapability, capability.Id);
                if (awsAccount != null)
                {
                    await _selfServiceDbContext.AwsAccounts.AddAsync(awsAccount, stoppingToken);
                }

                var capabilityMemberships = membershipLookup[capability.Id];
                await CreateMemberships(legacyCapability, capability, capabilityMemberships, memberLookup);

            }
            else
            {
                // update existing capability from legacy (v1)

                capability.Description = legacyCapability.Description ?? "";
                capability.Deleted = legacyCapability.Deleted;

                var context = legacyCapability.Contexts.SingleOrDefault();
                if (context != null)
                {
                    var awsAccount = await _selfServiceDbContext.AwsAccounts.SingleOrDefaultAsync(x => x.CapabilityId == capability.Id, cancellationToken: stoppingToken);
                    if (awsAccount == null)
                    {
                        awsAccount = ExtractAwsAccountFrom(legacyCapability, capability.Id);
                        if (awsAccount != null)
                        {
                            await _selfServiceDbContext.AwsAccounts.AddAsync(awsAccount, stoppingToken);
                        }
                    }

                    // TODO [jandr@2023-02-14]: we will disallow this kind of direct access to the properties of an aws account of a capability
                    // capability.AwsAccount.AccountId = context.AWSAccountId!;
                    // capability.AwsAccount.RoleArn = context.AWSRoleArn!;
                    // capability.AwsAccount.RoleEmail = context.AWSRoleEmail!;
                }

                var capabilityMemberships = membershipLookup[capability.Id];
                await CreateMemberships(legacyCapability, capability, capabilityMemberships, memberLookup);

                _logger.LogInformation("Updating capability {Id}", capability.Id);

            }
            await _selfServiceDbContext.SaveChangesAsync(stoppingToken);
        }
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
        return _selfServiceDbContext.Capabilities.ToListAsync(stoppingToken);
    }

    private Task<List<Membership>> GetAllMemberships(CancellationToken stoppingToken)
    {
        return _selfServiceDbContext.Memberships.ToListAsync(stoppingToken);
    }

    private Task<List<Member>> GetAllMembers(CancellationToken stoppingToken)
    {
        return _selfServiceDbContext.Members.ToListAsync(stoppingToken);
    }

    private async Task CreateMemberships(Models.Capability legacyCapability, Capability capability, IEnumerable<Membership> memberships, IDictionary<string, Member> members)
    {
        foreach (var legacyMembership in legacyCapability.Memberships)
        {
            var membership = memberships.FirstOrDefault(x => string.Equals(x.UserId, legacyMembership.Email, StringComparison.InvariantCultureIgnoreCase));
            if (membership != null)
            {
                // already has membership
                continue;
            }

            var newMembership = Membership.CreateFor(capability.Id, legacyMembership.Email, DateTime.UtcNow);
            await _selfServiceDbContext.Memberships.AddAsync(newMembership);

            if (members.ContainsKey(newMembership.UserId))
            {
                continue;
            }

            var newMember = new Member(newMembership.UserId, legacyMembership.Email, "");
            await _selfServiceDbContext.Members.AddAsync(newMember);

            members[newMembership.UserId] = newMember;
        }
    }

    private static AwsAccount? ExtractAwsAccountFrom(Models.Capability legacyCapability, CapabilityId capabilityId)
    {
        var context = legacyCapability.Contexts.SingleOrDefault();
        if (context == null )
        {
            return null;
        }

        return new AwsAccount
        (
            id: context.Id,
            capabilityId: capabilityId,
            accountId: string.IsNullOrWhiteSpace(context.AWSAccountId) ? null : RealAwsAccountId.Parse(context.AWSAccountId),
            roleEmail: context.AWSRoleEmail,
            requestedAt: DateTime.UtcNow,
            requestedBy: "SYSTEM"
        );
    }
}
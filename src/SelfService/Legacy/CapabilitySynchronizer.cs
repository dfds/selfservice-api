using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Legacy;

public class CapabilitySynchronizer
{
    private readonly ILogger<CapabilitySynchronizer> _logger;
    private readonly LegacyDbContext _legacyDbContext;
    private readonly SelfServiceDbContext _selfServiceDbContext;

    public CapabilitySynchronizer(ILogger<CapabilitySynchronizer> logger, LegacyDbContext legacyDbContext,
        SelfServiceDbContext selfServiceDbContext)
    {
        _logger = logger;
        _legacyDbContext = legacyDbContext;
        _selfServiceDbContext = selfServiceDbContext;
    }

    public Task Synchronize(CancellationToken stoppingToken)
    {
        //var legacyCapabilities = await GetAllLegacyCapabilities(stoppingToken);
        //var capabilities = await GetAllCapabilities(stoppingToken);
        //var members = await GetAllMembers(stoppingToken);
        //var memberLookup = members.ToDictionary(x => x.Id, StringComparer.InvariantCultureIgnoreCase);

        //_logger.LogInformation("Legacy Capabilities {Count}", legacyCapabilities.Count);
        //_logger.LogInformation("Capabilities {Count}", capabilities.Count);

        //foreach (var legacyCapability in legacyCapabilities.Where(x => !string.IsNullOrEmpty(x.RootId)))
        //{
        //    var capability = capabilities.FirstOrDefault(x => x.Id == legacyCapability.RootId);
        //    if (capability == null)
        //    {
        //        var awsAccount = ExtractAwsAccountFrom(legacyCapability);
        //        var memberships = CreateMemberships(legacyCapability, capability, memberLookup);

        //        capability = new Capability
        //        (
        //            id: legacyCapability.RootId!,
        //            name: legacyCapability.Name!,
        //            description: legacyCapability.Description ?? "",
        //            deleted: legacyCapability.Deleted.HasValue ? DateTime.SpecifyKind(legacyCapability.Deleted.Value, DateTimeKind.Utc) : null,
        //            awsAccount: awsAccount,
        //            createdAt: DateTime.UtcNow,
        //            createdBy: "SYSTEM"
        //        );

        //        await _selfServiceDbContext.Capabilities.AddAsync(capability, stoppingToken);
        //        _logger.LogInformation("Adding capability {Id}", capability.Id);


        //    }
        //    else
        //    {
        //        capability.Description = legacyCapability.Description ?? "";
        //        capability.Deleted = legacyCapability.Deleted;

        //        var context = legacyCapability.Contexts.SingleOrDefault();
        //        if (context != null && capability.AwsAccount != null)
        //        {
        //            // TODO [jandr@2023-02-14]: we will disallow this kind of direct access to the properties of an aws account of a capability
        //            capability.AwsAccount.AccountId = context.AWSAccountId!;
        //            capability.AwsAccount.RoleArn = context.AWSRoleArn!;
        //            capability.AwsAccount.RoleEmail = context.AWSRoleEmail!;
        //        }

        //        CreateMemberships(legacyCapability, capability, memberLookup);

        //        _logger.LogInformation("Updating capability {Id}", capability.Id);
        //    }
        //}

        //await _selfServiceDbContext.SaveChangesAsync(stoppingToken);

        return Task.CompletedTask;
    }

//    private Task<List<Models.Capability>> GetAllLegacyCapabilities(CancellationToken stoppingToken)
//    {
//        return _legacyDbContext.Capabilities
//            .Include(x => x.Memberships)
//            .Include(x => x.Contexts)
//            .ToListAsync(stoppingToken);
//    }

//    private Task<List<Capability>> GetAllCapabilities(CancellationToken stoppingToken)
//    {
//        return _selfServiceDbContext.Capabilities
//            .Include(x => x.Memberships)
//            .Include(x => x.AwsAccount)
//            .ToListAsync(stoppingToken);
//    }

//    private Task<List<Member>> GetAllMembers(CancellationToken stoppingToken)
//    {
//        return _selfServiceDbContext.Members.ToListAsync(stoppingToken);
//    }

//    private static IEnumerable<Membership> CreateMemberships(Models.Capability legacyCapability, Capability capability, IDictionary<string, Member> memberLookup)
//    {
//        var result = new LinkedList<Membership>();

//        foreach (var legacyMembership in legacyCapability.Memberships)
//        {
//            var membership = capability.Memberships.FirstOrDefault(x => string.Equals(x.UPN, legacyMembership.Email, StringComparison.InvariantCultureIgnoreCase));
//            if (membership != null)
//            {
//                // already has membership
//                continue;
//            }

//            var newMembership = CreateMembership(memberLookup, legacyMembership, capability);
//            result.AddLast(newMembership);
//        }

//        return result;
//    }

//    private static Membership CreateMembership(IDictionary<string, Member> memberLookup, Legacy.Models.Membership legacyMembership, Capability capability)
//    {
//        Member member;

//        if (memberLookup.ContainsKey(legacyMembership.Email))
//        {
//            member = memberLookup[legacyMembership.Email];
//        }
//        else
//        {
//            member = new Member
//            {
//                UPN = legacyMembership.Email,
//                Email = legacyMembership.Email,
//            };
//            memberLookup.Add(member.UPN, member);
//        }

//        return new Membership
//        {
//            CapabilityId = capability.Id,
//            UPN = legacyMembership.Email,
//            UserId = member,
//            CreatedAt = DateTime.UtcNow,
//        };
//    }

//    private static AwsAccount? ExtractAwsAccountFrom(Models.Capability legacyCapability)
//    {
//        var context = legacyCapability.Contexts.SingleOrDefault();
//        if (context == null)
//        {
//            return null;
//        }

//        return new AwsAccount
//        (
//            id: context.Id,
//            accountId: context.AWSAccountId!,
//            roleArn: context.AWSRoleArn!,
//            roleEmail: context.AWSRoleEmail!,
//            createdAt: DateTime.UtcNow,
//            createdBy: "SYSTEM"
//        );
//    }

}
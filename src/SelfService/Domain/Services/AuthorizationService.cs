using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IMembershipQuery _membershipQuery;
    private readonly IKafkaClusterAccessRepository _kafkaClusterAccessRepository;

    public AuthorizationService(ILogger<AuthorizationService> logger, IMembershipQuery membershipQuery, 
        IKafkaClusterAccessRepository kafkaClusterAccessRepository)
    {
        _logger = logger;
        _membershipQuery = membershipQuery;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
    }

    [Obsolete]
    public async Task<UserAccessLevelOptions> GetUserAccessLevelForCapability(UserId userId, CapabilityId capabilityId)
    {
        var hasActiveMembership = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        
        return hasActiveMembership
            ? UserAccessLevelOptions.ReadWrite
            : UserAccessLevelOptions.Read;
    }

    public async Task<bool> CanRead(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic || await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CanChange(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId);
        if (!isMemberOfOwningCapability)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> CanDelete(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            if (portalUser.Roles.Any(role => role == UserRole.CloudEngineer))
            {
                return true;
            }

            // NB: only cloud engineers are allowed to delete public topics
            return false;
        }

        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId);
        if (isMemberOfOwningCapability)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic || await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId))
        {
            return true;
        }
        
        return false;
    }

    public async Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic && await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId))
        {
            return true;
        }

        return false;
    }

    public async Task<bool> HasAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        var access = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        return access?.IsAccessGranted ?? false;
    }

    public async Task<bool> CanRead(UserId userId, MembershipApplication application)
    {
        var hasActiveMembership = await _membershipQuery.HasActiveMembership(userId, application.CapabilityId);

        if (hasActiveMembership)
        {
            return true;
        }

        return application.Applicant == userId;
    }

    public async Task<bool> CanApprove(UserId userId, MembershipApplication application)
    {
        return await _membershipQuery.HasActiveMembership(userId, application.CapabilityId);
    }
}
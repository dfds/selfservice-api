using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IAuthorizationService
{
    [Obsolete]
    Task<UserAccessLevelOptions> GetUserAccessLevelForCapability(UserId userId, CapabilityId capabilityId);

    Task<bool> CanRead(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanChange(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanDelete(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic);

    Task<bool> CanViewAccess(UserId userId, CapabilityId capabilityId);
    Task<bool> HasAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);

    Task<bool> CanRead(UserId userId, MembershipApplication application);
    Task<bool> CanApprove(UserId userId, MembershipApplication application);
}
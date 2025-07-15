using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IAuthorizationService
{
    Task<bool> CanAdd(UserId userId, CapabilityId capabilityId, KafkaClusterId clusterId);
    Task<bool> CanRead(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanChange(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanDelete(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanReadConsumers(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic);

    Task<bool> CanViewAccess(UserId userId, CapabilityId capabilityId);
    Task<bool> HasAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);

    Task<bool> CanRead(UserId userId, MembershipApplication application);
    Task<bool> CanApprove(UserId userId, MembershipApplication application);
    Task<bool> CanViewAwsAccount(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAwsAccountInformation(UserId userId, CapabilityId capabilityId);
    Task<bool> CanRequestAwsAccount(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAzureResources(UserId userId, CapabilityId capabilityId);
    Task<bool> CanRequestAzureResource(UserId userId, CapabilityId capabilityId, string environment);
    Task<bool> CanRequestAzureResources(UserId userId, CapabilityId capabilityId);

    Task<bool> CanLeave(UserId userId, CapabilityId capabilityId);
    Task<bool> CanApply(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAllApplications(UserId userId, CapabilityId capabilityId);
    Task<bool> CanDeleteCapability(UserId userId, CapabilityId capabilityId);
    bool CanViewDeletedCapabilities(PortalUser portalUser);
    bool CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser);
    Task<bool> CanGetSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId);
    bool CanBypassMembershipApprovals(PortalUser portalUser);
    Task<bool> CanDeleteMembershipApplication(
        PortalUser portalUser,
        UserId userId,
        MembershipApplicationId membershipApplicationId
    );
    Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId);
    Task<bool> CanSeeAwsAccountId(PortalUser portalUser, CapabilityId capabilityId);
    Task<bool> CanRetryCreatingMessageContract(PortalUser portalUser, MessageContractId messageContractId);
    Task<bool> CanSelfAssess(UserId userId, CapabilityId capabilityId);
    bool CanManageSelfAssessmentOptions(PortalUser portalUser);

    bool IsAuthorizedToCreateReleaseNotes(PortalUser portalUser);
    bool IsAuthorizedToUpdateReleaseNote(PortalUser portalUser);
    bool IsAuthorizedToToggleReleaseNoteIsActive(PortalUser portalUser);
    bool IsAuthorizedToListDraftReleaseNotes(PortalUser portalUser);
    bool IsAuthorizedToRemoveReleaseNote(PortalUser portalUser);
}

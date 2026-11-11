using SelfService.Domain.Models;

namespace SelfService.Domain.Services;

public interface IAuthorizationService
{
    Task<bool> CanAddTopic(UserId userId, CapabilityId capabilityId, KafkaClusterId clusterId);
    Task<bool> CanReadTopic(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanModifyTopic(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanDeleteTopic(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanReadConsumers(PortalUser portalUser, KafkaTopic kafkaTopic);
    Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic);

    Task<bool> CanViewKafkaClusterAccess(UserId userId, CapabilityId capabilityId);
    Task<bool> HasKafkaClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);

    Task<bool> CanReadMembershipApplications(UserId userId, MembershipApplication application);
    Task<bool> CanApproveMembershipApplications(UserId userId, MembershipApplication application);
    Task<bool> CanApproveMembershipApplications(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAwsAccount(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAwsAccountInformation(UserId userId, CapabilityId capabilityId);
    Task<bool> CanRequestAwsAccount(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAzureResources(UserId userId, CapabilityId capabilityId);
    Task<bool> CanRequestAzureResource(UserId userId, CapabilityId capabilityId, string environment);
    Task<bool> CanRequestAzureResources(UserId userId, CapabilityId capabilityId);

    Task<bool> CanLeave(UserId userId, CapabilityId capabilityId);
    Task<bool> CanRemoveMember(UserId requesterId, CapabilityId capabilityId);
    Task<bool> CanApply(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewAllApplications(UserId userId, CapabilityId capabilityId);
    Task<bool> CanDeleteCapability(UserId userId, CapabilityId capabilityId);
    Task<bool> CanViewDeletedCapabilities(PortalUser portalUser);
    Task<bool> CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser);
    Task<bool> CanManagePermissionMatrix(PortalUser portalUser);
    Task<bool> CanGetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId);
    Task<bool> CanSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId);
    Task<bool> CanBypassMembershipApprovals(PortalUser portalUser);
    Task<bool> CanBatchCreateCapabilities(PortalUser portalUser);
    Task<bool> CanDeleteMembershipApplication(
        PortalUser portalUser,
        UserId userId,
        MembershipApplicationId membershipApplicationId
    );
    Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId);
    Task<bool> CanSeeAwsAccountId(PortalUser portalUser, CapabilityId capabilityId);
    Task<bool> CanRetryCreatingMessageContract(PortalUser portalUser, MessageContractId messageContractId);
    Task<bool> CanSelfAssess(UserId userId, CapabilityId capabilityId);
    Task<bool> CanManageSelfAssessmentOptions(PortalUser portalUser);

    Task<bool> CanCreateDemoRecording(PortalUser portalUser);
    Task<bool> CanUpdateDemoRecording(PortalUser portalUser);
    Task<bool> CanDeleteDemoRecording(PortalUser portalUser);

    Task<bool> CanReadAllCopilotUsage(PortalUser portalUser);

    Task<bool> CanCreateEvent(PortalUser portalUser);
    Task<bool> CanUpdateEvent(PortalUser portalUser);
    Task<bool> CanDeleteEvent(PortalUser portalUser);

    Task<bool> CanCreateNewsItem(PortalUser portalUser);
    Task<bool> CanUpdateNewsItem(PortalUser portalUser);
    Task<bool> CanDeleteNewsItem(PortalUser portalUser);
    Task<bool> CanCreatePrivateAsset(PortalUser portalUser);

    Task<bool> IsAuthorizedToCreateReleaseNotes(PortalUser portalUser);
    Task<bool> IsAuthorizedToUpdateReleaseNote(PortalUser portalUser);
    Task<bool> IsAuthorizedToToggleReleaseNoteIsActive(PortalUser portalUser);
    Task<bool> IsAuthorizedToListDraftReleaseNotes(PortalUser portalUser);
    Task<bool> IsAuthorizedToRemoveReleaseNote(PortalUser portalUser);
    Task<bool> CanViewMembershipApplications(UserId userId, CapabilityId capabilityId);
    Task<bool> CanGetUserEmails(PortalUser portalUser);
    Task<bool> CanUnsetCapabilityTags(PortalUser portalUser);
}

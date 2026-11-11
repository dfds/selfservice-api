using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Tests.TestDoubles;

public class StubAuthorizationService : IAuthorizationService
{
    private readonly bool _authorized;

    public StubAuthorizationService(bool authorized)
    {
        _authorized = authorized;
    }

    public Task<bool> IsAuthorized(string userId, string permission)
    {
        return Task.FromResult(_authorized);
    }

    public async Task<bool> CanAddTopic(UserId userId, CapabilityId capabilityId, KafkaClusterId clusterId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanReadTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanModifyTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanDeleteTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await Task.FromResult(_authorized);
    }

    public Task<bool> CanViewDeletedCapabilities(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    private bool IsCloudEngineerEnabled(PortalUser portalUser)
    {
        return _authorized;
    }

    public async Task<bool> CanReadConsumers(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await Task.FromResult(_authorized);
    }

    // we don't have a lot of information about Kafka clusters in the RBAC system
    public async Task<bool> CanViewKafkaClusterAccess(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    // we don't have a lot of information about Kafka clusters in the RBAC system
    public async Task<bool> HasKafkaClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanReadMembershipApplications(UserId userId, MembershipApplication application)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanApproveMembershipApplications(UserId userId, MembershipApplication application)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanApproveMembershipApplications(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanViewAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanViewAwsAccount(UserId userId, AwsAccountId accountId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanViewAwsAccountInformation(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanRequestAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanViewAzureResources(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanRequestAzureResource(UserId userId, CapabilityId capabilityId, string environment)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanRequestAzureResources(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanLeave(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanRemoveMember(UserId requesterId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    // not covered by current RBAC rules
    public async Task<bool> CanApply(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanViewAllApplications(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanDeleteCapability(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public Task<bool> CanManagePermissionMatrix(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public async Task<bool> CanGetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public Task<bool> CanBypassMembershipApprovals(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public async Task<bool> CanDeleteMembershipApplication(
        PortalUser portalUser,
        UserId userId,
        MembershipApplicationId membershipApplicationId
    )
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanSeeAwsAccountId(PortalUser portalUser, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanRetryCreatingMessageContract(PortalUser portalUser, MessageContractId messageContractId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanSelfAssess(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public Task<bool> CanManageSelfAssessmentOptions(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanCreateDemoRecording(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanUpdateDemoRecording(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanDeleteDemoRecording(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanReadAllCopilotUsage(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> IsAuthorizedToCreateReleaseNotes(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> IsAuthorizedToUpdateReleaseNote(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> IsAuthorizedToToggleReleaseNoteIsActive(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> IsAuthorizedToListDraftReleaseNotes(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> IsAuthorizedToRemoveReleaseNote(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanCreateEvent(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanUpdateEvent(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanDeleteEvent(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanCreateNewsItem(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanUpdateNewsItem(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanDeleteNewsItem(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanCreatePrivateAsset(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public async Task<bool> CanViewMembershipApplications(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public Task<bool> CanGetUserEmails(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanUnsetCapabilityTags(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }

    public Task<bool> CanBatchCreateCapabilities(PortalUser portalUser)
    {
        return Task.FromResult(_authorized);
    }
}

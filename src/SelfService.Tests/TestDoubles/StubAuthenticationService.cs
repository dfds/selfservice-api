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

    public bool CanViewDeletedCapabilities(PortalUser portalUser)
    {
        return _authorized;
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

    public bool CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser)
    {
        return _authorized;
    }

    public async Task<bool> CanGetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public async Task<bool> CanSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        return await Task.FromResult(_authorized);
    }

    public bool CanBypassMembershipApprovals(PortalUser portalUser)
    {
        return _authorized;
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

    public bool CanManageSelfAssessmentOptions(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool IsAuthorizedToCreateReleaseNotes(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool IsAuthorizedToUpdateReleaseNote(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool IsAuthorizedToToggleReleaseNoteIsActive(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool IsAuthorizedToListDraftReleaseNotes(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool IsAuthorizedToRemoveReleaseNote(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool CanCreateDemo(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool CanUpdateDemo(PortalUser portalUser)
    {
        return _authorized;
    }

    public bool CanDeleteDemo(PortalUser portalUser)
    {
        return _authorized;
    }
}

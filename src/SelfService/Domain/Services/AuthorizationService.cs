using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IMembershipQuery _membershipQuery;
    private readonly IKafkaClusterAccessRepository _kafkaClusterAccessRepository;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IAzureResourceRepository _azureResourceRepository;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;
    private readonly IRbacApplicationService _rbacApplicationService;

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        IMembershipQuery membershipQuery,
        IKafkaClusterAccessRepository kafkaClusterAccessRepository,
        IAwsAccountRepository awsAccountRepository,
        IAzureResourceRepository azureResourceRepository,
        IMessageContractRepository messageContractRepository,
        IKafkaTopicRepository kafkaTopicRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        IRbacApplicationService rbacApplicationService
    )
    {
        _logger = logger;
        _membershipQuery = membershipQuery;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
        _rbacApplicationService = rbacApplicationService;
        _awsAccountRepository = awsAccountRepository;
        _azureResourceRepository = azureResourceRepository;
        _messageContractRepository = messageContractRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
    }

    public async Task<bool> CanAddTopic(UserId userId, CapabilityId capabilityId, KafkaClusterId clusterId)
    {
        var canCreateTopics = await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "create",
            capabilityId
        );

        var hasClusterAccess =
            (await _kafkaClusterAccessRepository.FindBy(capabilityId, clusterId))?.IsAccessGranted ?? false;
        return hasClusterAccess && canCreateTopics;
    }

    public async Task<bool> CanReadTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return true;
        }

        return await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "read-private",
            kafkaTopic.CapabilityId
        );
    }

    public async Task<bool> CanModifyTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "update",
            kafkaTopic.CapabilityId
        );
    }

    public async Task<bool> CanDeleteTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return await HasPermission(
                portalUser.Id,
                RbacAccessType.Capability,
                RbacNamespace.Topics,
                "delete-public",
                kafkaTopic.CapabilityId
            );
        }

        return await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "delete",
            kafkaTopic.CapabilityId
        );
    }

    public bool CanViewDeletedCapabilities(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Capability, "view-deleted-capabilities");
    }

    public bool CanUnsetCapabilityTags(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Capability, "unset-capability-tags");
    }

    public bool CanCreateDemoRecording(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Demos, "create");
    }

    public bool CanUpdateDemoRecording(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Demos, "update");
    }

    public bool CanDeleteDemoRecording(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Demos, "delete");
    }

    public bool CanReadDemoSignups(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Demos, "read-signups");
    }

    private bool HasPermission(
        PortalUser portalUser,
        RbacAccessType scope,
        RbacNamespace permissionNamespace,
        string permissionName,
        string resourceId = ""
    )
    {
        try
        {
            return HasPermission(portalUser.Id, scope, permissionNamespace, permissionName, resourceId)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to evaluate permission {Scope} {Namespace}/{PermissionName} for user {UserId}",
                scope,
                permissionNamespace,
                permissionName,
                portalUser.Id
            );
            return false;
        }
    }

    private async Task<bool> HasPermission(
        string userId,
        RbacAccessType scope,
        RbacNamespace permissionNamespace,
        string permissionName,
        string resourceId = ""
    )
    {
        var permissionsToCheck = new List<Permission>
        {
            new()
            {
                Namespace = permissionNamespace,
                Name = permissionName,
                AccessType = scope,
            },
        };

        // Any non-global scope should also be satisfied by equivalent global grants.
        if (scope != RbacAccessType.Global)
        {
            permissionsToCheck.Add(
                new()
                {
                    Namespace = permissionNamespace,
                    Name = permissionName,
                    AccessType = RbacAccessType.Global,
                }
            );
        }

        return (await _rbacApplicationService.IsUserPermitted(userId, permissionsToCheck, resourceId)).Permitted();
    }

    public async Task<bool> CanReadConsumers(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return true;
        }

        return await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "read-private",
            kafkaTopic.CapabilityId
        );
    }

    public async Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return true;
        }

        return await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "read-private",
            kafkaTopic.CapabilityId
        );
    }

    public async Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "update",
            kafkaTopic.CapabilityId
        );
    }

    // we don't have information about Kafka clusters in the RBAC system
    // so for now we allow all users to view Kafka cluster access
    public async Task<bool> CanViewKafkaClusterAccess(UserId userId, CapabilityId capabilityId)
    {
        return await Task.FromResult(true);
    }

    // we don't have a lot of information about Kafka clusters in the RBAC system
    public async Task<bool> HasKafkaClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        var access = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        return access?.IsAccessGranted ?? false;
    }

    public async Task<bool> CanReadMembershipApplications(UserId userId, MembershipApplication application)
    {
        var ownsApplication = application.Applicant == userId;
        var hasReadRequestsPermission = await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "read-requests",
            application.CapabilityId
        );

        return ownsApplication || hasReadRequestsPermission;
    }

    public async Task<bool> CanApproveMembershipApplications(UserId userId, MembershipApplication application)
    {
        return await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "manage-requests",
            application.CapabilityId
        );
    }

    public async Task<bool> CanApproveMembershipApplications(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "manage-requests",
            capabilityId
        );
    }

    public async Task<bool> CanViewAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        var canReadAwsAccount = await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Aws,
            "read",
            capabilityId
        );

        return (await _awsAccountRepository.Exists(capabilityId)) && canReadAwsAccount;
    }

    public async Task<bool> CanViewAwsAccount(UserId userId, AwsAccountId accountId)
    {
        var account = await _awsAccountRepository.Get(accountId);
        if (account == null)
        {
            return false;
        }
        return await HasPermission(userId, RbacAccessType.Capability, RbacNamespace.Aws, "read", account.CapabilityId);
    }

    public async Task<bool> CanViewAwsAccountInformation(UserId userId, CapabilityId capabilityId)
    {
        var account = await _awsAccountRepository.FindBy(capabilityId);
        if (account is null)
            return false;

        if (!(account.Status == AwsAccountStatus.Completed))
        {
            return false;
        }

        return await HasPermission(userId, RbacAccessType.Capability, RbacNamespace.Aws, "read", account.CapabilityId);
    }

    public async Task<bool> CanRequestAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        var canCreateAwsAccount = await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Aws,
            "create",
            capabilityId
        );

        return (!await _awsAccountRepository.Exists(capabilityId)) && canCreateAwsAccount;
    }

    public async Task<bool> CanViewAzureResources(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(userId, RbacAccessType.Capability, RbacNamespace.Azure, "read", capabilityId);
    }

    public async Task<bool> CanRequestAzureResource(UserId userId, CapabilityId capabilityId, string environment)
    {
        return await HasPermission(userId, RbacAccessType.Capability, RbacNamespace.Azure, "create", capabilityId);
    }

    public async Task<bool> CanRequestAzureResources(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(userId, RbacAccessType.Capability, RbacNamespace.Azure, "create", capabilityId);
    }

    public async Task<bool> CanLeave(UserId userId, CapabilityId capabilityId)
    {
        var capabilityRoleGrants = await _rbacApplicationService.GetRoleGrantsForCapability(capabilityId);

        var userIsMember = capabilityRoleGrants.Any(rg => rg.AssignedEntityId == userId);
        if (!userIsMember)
        {
            return false;
        }

        var ownerRoleId = _rbacApplicationService
            .GetAssignableRoles()
            .Result.Where(r => r.Name == "Owner")
            .Select(r => r.Id)
            .ToList()
            .FirstOrDefault();

        var numberOfOwners = capabilityRoleGrants.Count(rg => rg.RoleId == ownerRoleId);
        var userIsOwner = capabilityRoleGrants.Any(rg => rg.AssignedEntityId == userId && rg.RoleId == ownerRoleId);

        if (userIsOwner && numberOfOwners <= 1)
        {
            return false;
        }

        return true;
    }

    // not covered by current RBAC rules
    public async Task<bool> CanApply(UserId userId, CapabilityId capabilityId)
    {
        return !await _membershipQuery.HasActiveMembership(userId, capabilityId)
            && !await _membershipQuery.HasActiveMembershipApplication(userId, capabilityId);
    }

    public async Task<bool> CanViewAllApplications(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "read-requests",
            capabilityId
        );
    }

    public async Task<bool> CanDeleteCapability(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "request-deletion",
            capabilityId
        );
    }

    public bool CanManagePermissionMatrix(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Rbac, "update");
    }

    public bool CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser)
    {
        return HasPermission(
            portalUser,
            RbacAccessType.Global,
            RbacNamespace.SystemAdmin,
            "synchronize-aws-ecr-and-database-ecr"
        );
    }

    public async Task<bool> CanGetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        var hasPermission = await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.TagsAndMetadata,
            "read",
            capabilityId
        );

        return hasPermission;
    }

    public async Task<bool> CanSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        var hasCreatePermission = await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.TagsAndMetadata,
            "create",
            capabilityId
        );

        var hasUpdatePermission = await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.TagsAndMetadata,
            "update",
            capabilityId
        );

        return hasCreatePermission || hasUpdatePermission;
    }

    public bool CanBypassMembershipApprovals(PortalUser portalUser)
    {
        return HasPermission(
            portalUser,
            RbacAccessType.Global,
            RbacNamespace.Capability,
            "bypass-membership-approvals"
        );
    }

    public bool CanBatchCreateCapabilities(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Capability, "batch-create-capabilities");
    }

    public async Task<bool> CanDeleteMembershipApplication(
        PortalUser portalUser,
        UserId userId,
        MembershipApplicationId membershipApplicationId
    )
    {
        var membershipApp = await _membershipApplicationRepository.Get(membershipApplicationId);
        var hasPermission = await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "manage-requests",
            membershipApp.CapabilityId
        );
        var isApplicant = membershipApp.Applicant == userId;

        return isApplicant || hasPermission;
    }

    public async Task<bool> CanRemoveMember(UserId requesterId, CapabilityId capabilityId)
    {
        return await HasPermission(
            requesterId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "remove-member",
            capabilityId
        );
    }

    public async Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "invite-member",
            capabilityId
        );
    }

    public async Task<bool> CanViewMembershipApplications(UserId userId, CapabilityId capabilityId)
    {
        return await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.Capability,
            "read-requests",
            capabilityId
        );
    }

    public async Task<bool> CanSeeAwsAccountId(PortalUser portalUser, CapabilityId capabilityId)
    {
        return await HasPermission(portalUser.Id, RbacAccessType.Capability, RbacNamespace.Aws, "read", capabilityId);
    }

    public async Task<bool> CanRetryCreatingMessageContract(PortalUser portalUser, MessageContractId messageContractId)
    {
        var messageContract = await _messageContractRepository.Get(messageContractId);
        if (messageContract.Status != MessageContractStatus.Failed)
        {
            return false;
        }
        var kafkaTopic = await _kafkaTopicRepository.Get(messageContract.KafkaTopicId);
        var canCreateMessageContract = await HasPermission(
            portalUser.Id,
            RbacAccessType.Capability,
            RbacNamespace.Topics,
            "update",
            kafkaTopic.CapabilityId
        );
        bool hasGlobalRetryPermission = HasPermission(
            portalUser,
            RbacAccessType.Global,
            RbacNamespace.Topics,
            "retry-creating-message-contract"
        );

        return hasGlobalRetryPermission || canCreateMessageContract;
    }

    public async Task<bool> CanSelfAssess(UserId userId, CapabilityId capabilityId)
    {
        var canCreate = await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.TagsAndMetadata,
            "create",
            capabilityId
        );
        var canUpdate = await HasPermission(
            userId,
            RbacAccessType.Capability,
            RbacNamespace.TagsAndMetadata,
            "update",
            capabilityId
        );

        return canCreate && canUpdate;
    }

    public bool CanManageSelfAssessmentOptions(PortalUser portalUser)
    {
        return HasPermission(
            portalUser,
            RbacAccessType.Global,
            RbacNamespace.SelfAssessment,
            "manage-self-assessment-options"
        );
    }

    public bool IsAuthorizedToCreateReleaseNotes(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.ReleaseNotes, "create");
    }

    public bool IsAuthorizedToUpdateReleaseNote(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.ReleaseNotes, "update");
    }

    public bool IsAuthorizedToToggleReleaseNoteIsActive(PortalUser portalUser)
    {
        return HasPermission(
            portalUser,
            RbacAccessType.Global,
            RbacNamespace.ReleaseNotes,
            "toggle-release-note-is-active"
        );
    }

    public bool IsAuthorizedToListDraftReleaseNotes(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.ReleaseNotes, "list-draft-release-notes");
    }

    public bool IsAuthorizedToRemoveReleaseNote(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.ReleaseNotes, "delete");
    }

    public bool CanCreateEvent(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Events, "create");
    }

    public bool CanUpdateEvent(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Events, "update");
    }

    public bool CanDeleteEvent(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Events, "delete");
    }

    public bool CanCreateNewsItem(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.News, "create");
    }

    public bool CanUpdateNewsItem(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.News, "update");
    }

    public bool CanDeleteNewsItem(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.News, "delete");
    }

    public bool CanGetUserEmails(PortalUser portalUser)
    {
        return HasPermission(portalUser, RbacAccessType.Global, RbacNamespace.Users, "read");
    }
}

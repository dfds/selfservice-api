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
    private readonly IHttpContextAccessor _httpContextAccessor;
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
        IHttpContextAccessor httpContextAccessor,
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
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CanAddTopic(UserId userId, CapabilityId capabilityId, KafkaClusterId clusterId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);

        /*
        var canCreateTopics = (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */

        var hasClusterAccess =
            (await _kafkaClusterAccessRepository.FindBy(capabilityId, clusterId))?.IsAccessGranted ?? false;
        return hasClusterAccess && isMemberOfOwningCapability; // canCreateTopics;
    }

    public async Task<bool> CanReadTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        /*
        if (kafkaTopic.IsPublic)
        {
            return (
                await _rbacApplicationService.IsUserPermitted(
                    portalUser.Id,
                    new List<Permission>
                    {
                        new()
                        {
                            Namespace = RbacNamespace.Topics,
                            Name = "read-public",
                            AccessType = RbacAccessType.Capability,
                        },
                    },
                    kafkaTopic.CapabilityId
                )
            ).Permitted();
        }

        return (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "read-private",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
        */
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(
            portalUser.Id,
            kafkaTopic.CapabilityId
        );
        return (isMemberOfOwningCapability && kafkaTopic.IsPrivate) || kafkaTopic.IsPublic;
    }

    public async Task<bool> CanModifyTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "update",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
    }

    public async Task<bool> CanDeleteTopic(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return (
                await _rbacApplicationService.IsUserPermitted(
                    portalUser.Id,
                    new List<Permission>
                    {
                        new()
                        {
                            Namespace = RbacNamespace.TopicsPublic,
                            Name = "delete",
                            AccessType = RbacAccessType.Capability,
                        },
                    },
                    kafkaTopic.CapabilityId
                )
            ).Permitted();
        }
        return (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "delete",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
    }

    public bool CanViewDeletedCapabilities(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    private bool IsCloudEngineerEnabled(PortalUser portalUser)
    {
        if (portalUser.Roles.Any(role => role == UserRole.CloudEngineer))
        {
            if (
                _httpContextAccessor.HttpContext != null
                && !_httpContextAccessor.HttpContext.Items.ContainsKey("userPermissions")
            )
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> CanReadConsumers(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return (
                await _rbacApplicationService.IsUserPermitted(
                    portalUser.Id,
                    new List<Permission>
                    {
                        new()
                        {
                            Namespace = RbacNamespace.Topics,
                            Name = "read-public",
                            AccessType = RbacAccessType.Capability,
                        },
                    },
                    kafkaTopic.CapabilityId
                )
            ).Permitted();
        }

        return (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "read-private",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
    }

    public async Task<bool> CanReadMessageContracts(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return (
                await _rbacApplicationService.IsUserPermitted(
                    portalUser.Id,
                    new List<Permission>
                    {
                        new()
                        {
                            Namespace = RbacNamespace.Topics,
                            Name = "read-public",
                            AccessType = RbacAccessType.Capability,
                        },
                    },
                    kafkaTopic.CapabilityId
                )
            ).Permitted();
        }

        return (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "read-private",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
    }

    public async Task<bool> CanAddMessageContract(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        return (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "update",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
    }

    // we don't have a lot of information about Kafka clusters in the RBAC system
    public async Task<bool> CanViewKafkaClusterAccess(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    // we don't have a lot of information about Kafka clusters in the RBAC system
    public async Task<bool> HasKafkaClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId)
    {
        var access = await _kafkaClusterAccessRepository.FindBy(capabilityId, kafkaClusterId);
        return access?.IsAccessGranted ?? false;
    }

    public async Task<bool> CanReadMembershipApplications(UserId userId, MembershipApplication application)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, application.CapabilityId);
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.CapabilityMembershipManagement,
                        Name = "read-requests",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                application.CapabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public async Task<bool> CanApproveMembershipApplications(UserId userId, MembershipApplication application)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, application.CapabilityId);
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.CapabilityMembershipManagement,
                        Name = "manage-requests",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                application.CapabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public async Task<bool> CanViewAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        /*
        var canReadAwsAccount = (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Aws,
                        Name = "read",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        return (await _awsAccountRepository.Exists(capabilityId)) && isMemberOfOwningCapability; //canReadAwsAccount;
    }

    public async Task<bool> CanViewAwsAccount(UserId userId, AwsAccountId accountId)
    {
        var account = await _awsAccountRepository.Get(accountId);
        if (account == null)
        {
            return false;
        }
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, account.CapabilityId);
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Aws,
                        Name = "read",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                account.CapabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public async Task<bool> CanViewAwsAccountInformation(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        var account = await _awsAccountRepository.FindBy(capabilityId);
        if (account is null)
            return false;

        if (!(account.Status == AwsAccountStatus.Completed))
        {
            return false;
        }

        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Aws,
                        Name = "read",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                account.CapabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public async Task<bool> CanRequestAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        var canCreateAwsAccount = isMemberOfOwningCapability;
        /*
        var canCreateAwsAccount = (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Aws,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        return (!await _awsAccountRepository.Exists(capabilityId)) && canCreateAwsAccount;
    }

    public async Task<bool> CanViewAzureResources(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        return isMemberOfOwningCapability;
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Azure,
                        Name = "read",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
    }

    public async Task<bool> CanRequestAzureResource(UserId userId, CapabilityId capabilityId, string environment)
    {
        // should we use the environment for anything? It is already checked for before calling this function
        // as is we could consolidate the two 'identical' CanRequestAzureResource(s) methods
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        return isMemberOfOwningCapability;
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Azure,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
    }

    public async Task<bool> CanRequestAzureResources(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        return isMemberOfOwningCapability;
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Azure,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
    }

    public async Task<bool> CanLeave(UserId userId, CapabilityId capabilityId)
    {
        var ownerRoleId = _rbacApplicationService
            .GetAssignableRoles()
            .Result.Where(r => r.Name == "Owner")
            .Select(r => r.Id)
            .ToList()
            .FirstOrDefault();

        var capabilityRoleGrants = await _rbacApplicationService.GetRoleGrantsForCapability(capabilityId);
        var numberOfOwners = capabilityRoleGrants.Count(rg => rg.RoleId == ownerRoleId);
        var userIsOwner = capabilityRoleGrants.Any(rg => rg.AssignedEntityId == userId && rg.RoleId == ownerRoleId);

        if (userIsOwner && numberOfOwners <= 1)
        {
            return false;
        }

        var hasActiveMembership = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        var hasMultipleMembers = await _membershipQuery.HasMultipleMembers(capabilityId);

        // return hasActiveMembership && hasMultipleMembers;
        return false;
    }

    // not covered by current RBAC rules
    public async Task<bool> CanApply(UserId userId, CapabilityId capabilityId)
    {
        return !await _membershipQuery.HasActiveMembership(userId, capabilityId)
            && !await _membershipQuery.HasActiveMembershipApplication(userId, capabilityId);
    }

    public async Task<bool> CanViewAllApplications(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.CapabilityMembershipManagement,
                        Name = "read-requests",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public async Task<bool> CanDeleteCapability(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.CapabilityManagement,
                        Name = "request-deletion",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public bool CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public async Task<bool> CanGetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(portalUser.Id, capabilityId);
        var cloudEngineer = IsCloudEngineerEnabled(portalUser);
        /*
        var hasPermission = (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.TagsAndMetadata,
                        Name = "read",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */

        return isMemberOfOwningCapability || cloudEngineer;
    }

    public async Task<bool> CanSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(portalUser.Id, capabilityId);
        var cloudEngineer = IsCloudEngineerEnabled(portalUser);
        /*
        var hasCreatePermission = (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.TagsAndMetadata,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        var hasUpdatePermission = (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.TagsAndMetadata,
                        Name = "update",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability || cloudEngineer; //hasCreatePermission || hasUpdatePermission || cloudEngineer;
    }

    public bool CanBypassMembershipApprovals(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public async Task<bool> CanDeleteMembershipApplication(
        PortalUser portalUser,
        UserId userId,
        MembershipApplicationId membershipApplicationId
    )
    {
        var membershipApp = await _membershipApplicationRepository.Get(membershipApplicationId);
        var hasPermission = (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.CapabilityMembershipManagement,
                        Name = "manage-requests",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                membershipApp.CapabilityId
            )
        ).Permitted();
        var isApplicant = membershipApp.Applicant == userId;

        return isApplicant || hasPermission || IsCloudEngineerEnabled(portalUser);
    }

    public async Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(userId, capabilityId);
        /*
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.CapabilityMembershipManagement,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        return isMemberOfOwningCapability;
    }

    public async Task<bool> CanSeeAwsAccountId(PortalUser portalUser, CapabilityId capabilityId)
    {
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(portalUser.Id, capabilityId);
        var isCloudEngineer = IsCloudEngineerEnabled(portalUser);
        /*
        var canReadAwsAccount = (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Aws,
                        Name = "read",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
        */
        //bool isCloudEngineer = IsCloudEngineerEnabled(portalUser);

        return isMemberOfOwningCapability || isCloudEngineer;
    }

    public async Task<bool> CanRetryCreatingMessageContract(PortalUser portalUser, MessageContractId messageContractId)
    {
        var messageContract = await _messageContractRepository.Get(messageContractId);
        if (messageContract.Status != MessageContractStatus.Failed)
        {
            return false;
        }
        var kafkaTopic = await _kafkaTopicRepository.Get(messageContract.KafkaTopicId);
        var canCreateMessageContract = (
            await _rbacApplicationService.IsUserPermitted(
                portalUser.Id,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.Topics,
                        Name = "update",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                kafkaTopic.CapabilityId
            )
        ).Permitted();
        bool isCloudEngineer = IsCloudEngineerEnabled(portalUser);

        return isCloudEngineer || canCreateMessageContract;
    }

    public async Task<bool> CanSelfAssess(UserId userId, CapabilityId capabilityId)
    {
        return (
            await _rbacApplicationService.IsUserPermitted(
                userId,
                new List<Permission>
                {
                    new()
                    {
                        Namespace = RbacNamespace.TagsAndMetadata,
                        Name = "create",
                        AccessType = RbacAccessType.Capability,
                    },
                    new()
                    {
                        Namespace = RbacNamespace.TagsAndMetadata,
                        Name = "update",
                        AccessType = RbacAccessType.Capability,
                    },
                },
                capabilityId
            )
        ).Permitted();
    }

    public bool CanManageSelfAssessmentOptions(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool IsAuthorizedToCreateReleaseNotes(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool IsAuthorizedToUpdateReleaseNote(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool IsAuthorizedToToggleReleaseNoteIsActive(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool IsAuthorizedToListDraftReleaseNotes(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool IsAuthorizedToRemoveReleaseNote(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool CanCreateDemo(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool CanUpdateDemo(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public bool CanDeleteDemo(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }
}

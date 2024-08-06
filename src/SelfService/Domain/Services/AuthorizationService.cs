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

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        IMembershipQuery membershipQuery,
        IKafkaClusterAccessRepository kafkaClusterAccessRepository,
        IAwsAccountRepository awsAccountRepository,
        IAzureResourceRepository azureResourceRepository,
        IMessageContractRepository messageContractRepository,
        IKafkaTopicRepository kafkaTopicRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _logger = logger;
        _membershipQuery = membershipQuery;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
        _awsAccountRepository = awsAccountRepository;
        _azureResourceRepository = azureResourceRepository;
        _messageContractRepository = messageContractRepository;
        _kafkaTopicRepository = kafkaTopicRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> CanAdd(UserId userId, CapabilityId capabilityId, KafkaClusterId clusterId)
    {
        if (!await _membershipQuery.HasActiveMembership(userId, capabilityId))
        {
            return false;
        }

        var kafkaClusterAccess = await _kafkaClusterAccessRepository.FindBy(capabilityId, clusterId);
        return kafkaClusterAccess?.IsAccessGranted ?? false;
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
        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(
            portalUser.Id,
            kafkaTopic.CapabilityId
        );
        if (!isMemberOfOwningCapability)
        {
            return false;
        }

        return true;
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

    public async Task<bool> CanDelete(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (kafkaTopic.IsPublic)
        {
            return IsCloudEngineerEnabled(portalUser);
        }

        var isMemberOfOwningCapability = await _membershipQuery.HasActiveMembership(
            portalUser.Id,
            kafkaTopic.CapabilityId
        );
        if (isMemberOfOwningCapability)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CanReadConsumers(PortalUser portalUser, KafkaTopic kafkaTopic)
    {
        if (await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId))
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

    public async Task<bool> CanViewAccess(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
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

    public async Task<bool> CanViewAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId)
            && await _awsAccountRepository.Exists(capabilityId);
    }

    public async Task<bool> CanRequestAwsAccount(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId)
            && !await _awsAccountRepository.Exists(capabilityId);
    }

    public async Task<bool> CanViewAzureResources(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    public async Task<bool> CanRequestAzureResource(UserId userId, CapabilityId capabilityId, string environment)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    public async Task<bool> CanRequestAzureResources(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    public async Task<bool> CanLeave(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId)
            && await _membershipQuery.HasMultipleMembers(capabilityId);
    }

    public async Task<bool> CanApply(UserId userId, CapabilityId capabilityId)
    {
        return !await _membershipQuery.HasActiveMembership(userId, capabilityId)
            && !await _membershipQuery.HasActiveMembershipApplication(userId, capabilityId);
    }

    public async Task<bool> CanViewAllApplications(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    public async Task<bool> CanDeleteCapability(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    public bool CanSynchronizeAwsECRAndDatabaseECR(PortalUser portalUser)
    {
        return IsCloudEngineerEnabled(portalUser);
    }

    public async Task<bool> CanGetSetCapabilityJsonMetadata(PortalUser portalUser, CapabilityId capabilityId)
    {
        var cloudEngineer = IsCloudEngineerEnabled(portalUser);

        return await _membershipQuery.HasActiveMembership(portalUser.Id, capabilityId) || cloudEngineer;
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
        var hasMembership = await _membershipQuery.HasActiveMembership(userId, membershipApp.CapabilityId);

        return hasMembership || IsCloudEngineerEnabled(portalUser);
    }

    public async Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }

    public async Task<bool> CanSeeAwsAccountId(PortalUser portalUser, CapabilityId capabilityId)
    {
        bool isMember = await _membershipQuery.HasActiveMembership(portalUser.Id, capabilityId);
        bool isCloudEngineer = IsCloudEngineerEnabled(portalUser);

        return isMember || isCloudEngineer;
    }

    public async Task<bool> CanRetryCreatingMessageContract(PortalUser portalUser, MessageContractId messageContractId)
    {
        var messageContract = await _messageContractRepository.Get(messageContractId);
        if (messageContract.Status != MessageContractStatus.Failed)
        {
            return false;
        }
        var kafkaTopic = await _kafkaTopicRepository.Get(messageContract.KafkaTopicId);
        bool isMember = await _membershipQuery.HasActiveMembership(portalUser.Id, kafkaTopic.CapabilityId);
        bool isCloudEngineer = IsCloudEngineerEnabled(portalUser);

        return (kafkaTopic.IsPrivate && isMember) || isCloudEngineer || !kafkaTopic.IsPrivate;
    }

    public async Task<bool> CanClaim(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }
}

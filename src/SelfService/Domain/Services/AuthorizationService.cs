﻿using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly ILogger<AuthorizationService> _logger;
    private readonly IMembershipQuery _membershipQuery;
    private readonly IKafkaClusterAccessRepository _kafkaClusterAccessRepository;
    private readonly IAwsAccountRepository _awsAccountRepository;

    public AuthorizationService(
        ILogger<AuthorizationService> logger,
        IMembershipQuery membershipQuery,
        IKafkaClusterAccessRepository kafkaClusterAccessRepository,
        IAwsAccountRepository awsAccountRepository
    )
    {
        _logger = logger;
        _membershipQuery = membershipQuery;
        _kafkaClusterAccessRepository = kafkaClusterAccessRepository;
        _awsAccountRepository = awsAccountRepository;
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
        if (portalUser.Roles.Any(role => role == UserRole.CloudEngineer))
        {
            return true;
        }
        return false;
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
        return portalUser.Roles.Any(role => role == UserRole.CloudEngineer);
    }

    public bool CanGetSetCapabilityJsonMetadata(PortalUser portalUser)
    {
        return portalUser.Roles.Any(role => role == UserRole.CloudEngineer);
    }

    public bool CanBypassMembershipApprovals(PortalUser portalUser)
    {
        return portalUser.Roles.Any(role => role == UserRole.CloudEngineer);
    }

    public async Task<bool> CanInviteToCapability(UserId userId, CapabilityId capabilityId)
    {
        return await _membershipQuery.HasActiveMembership(userId, capabilityId);
    }
}

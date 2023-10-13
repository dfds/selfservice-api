using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;

namespace SelfService.Application;

public class MembershipApplicationService : IMembershipApplicationService
{
    private readonly ILogger<MembershipApplicationService> _logger;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly SystemTime _systemTime;
    private readonly IMembershipQuery _membershipQuery;
    private readonly IMembershipApplicationDomainService _membershipApplicationDomainService;

    public MembershipApplicationService(
        ILogger<MembershipApplicationService> logger,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        IAuthorizationService authorizationService,
        SystemTime systemTime,
        IMembershipQuery membershipQuery,
        IMembershipApplicationDomainService membershipApplicationDomainService
    )
    {
        _logger = logger;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _authorizationService = authorizationService;
        _systemTime = systemTime;
        _membershipQuery = membershipQuery;
        _membershipApplicationDomainService = membershipApplicationDomainService;
    }

    public async Task CreateAndAddMembership(CapabilityId capabilityId, UserId userId){
        var newMembership = Membership.CreateFor(
            capabilityId: capabilityId,
            userId: userId,
            createdAt: _systemTime.Now
        );

        await _membershipRepository.Add(newMembership);

        _logger.LogInformation(
            "User {UserId} has joined capability {CapabilityId}",
            userId,
            capabilityId
        );
    }

    [TransactionalBoundary, Outboxed]
    public async Task AddCreatorAsInitialMember(CapabilityId capabilityId, UserId creatorId)
    {
        _logger.LogInformation(
            "Creator {CreatorId} is added as initial member to capability {CapabilityId}",
            creatorId,
            capabilityId
        );
        await CreateAndAddMembership(capabilityId, creatorId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MembershipId> AcceptApplication(MembershipApplicationId applicationId)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(AcceptApplication),
            GetType().FullName
        );

        var membershipApplication = await _membershipApplicationRepository.Get(applicationId);
        if (!membershipApplication.IsFinalized)
        {
            _logger.LogError(
                "Membership application {MembershipApplicationId} has status {MembershipApplicationStatus} and is not finalized",
                applicationId,
                membershipApplication.Status
            );
            throw new Exception(
                $"Cannot start a new membership from a membership application (#{applicationId}) that is not finalized."
            );
        }

        var capabilityExists = await _capabilityRepository.Exists(membershipApplication.CapabilityId);
        if (!capabilityExists)
        {
            _logger.LogError("Could not find a capability with id {CapabilityId}", membershipApplication.CapabilityId);
            throw EntityNotFoundException<Capability>.UsingId(membershipApplication.CapabilityId);
        }

        var newMembership = Membership.CreateFor(
            capabilityId: membershipApplication.CapabilityId,
            userId: membershipApplication.Applicant,
            createdAt: _systemTime.Now
        );

        await CreateAndAddMembership(membershipApplication.CapabilityId, membershipApplication.Applicant);
        return newMembership.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MembershipApplicationId> SubmitMembershipApplication(CapabilityId capabilityId, UserId userId)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType} for {CurrentUser}",
            nameof(SubmitMembershipApplication),
            GetType().FullName,
            userId
        );

        if (!await _capabilityRepository.Exists(capabilityId))
        {
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        if (await _membershipQuery.HasActiveMembership(userId, capabilityId))
        {
            throw new AlreadyHasActiveMembershipException(
                $"User \"{userId}\" is already member of \"{capabilityId}\"."
            );
        }

        // if (_authorizationService.CanBypassMembershipApprovals(userId)){
        //      await creatAndAddMembership(userId, capabilityId);
        // }

        var existingApplication = await _membershipApplicationRepository.FindPendingBy(capabilityId, userId);
        if (existingApplication != null)
        {
            // NOTE [jandr@2023-03-02]: should this be an EntityAlreadyExistsException instead??
            throw new PendingMembershipApplicationAlreadyExistsException(
                $"User \"{userId}\" already has a pending membership application for capability \"{capabilityId}\""
            );
        }

        var application = MembershipApplication.New(capabilityId, userId, _systemTime.Now);
        await _membershipApplicationRepository.Add(application);

        return application.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task TryFinalizeMembershipApplication(MembershipApplicationId applicationId)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(TryFinalizeMembershipApplication),
            GetType().FullName
        );

        var application = await _membershipApplicationRepository.Get(applicationId);

        if (_membershipApplicationDomainService.CanBeFinalized(application))
        {
            application.FinalizeApprovals();

            _logger.LogInformation(
                "Finalized membership application approvals on {MembershipApplicationId} for {CapabilityId}",
                application.Id,
                application.CapabilityId
            );
        }
        else
        {
            _logger.LogDebug(
                "Could not yet finalize membership application approvals for {MembershipApplicationId}",
                application.Id
            );
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task ApproveMembershipApplication(MembershipApplicationId applicationId, UserId approvedBy)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(ApproveMembershipApplication),
            GetType().FullName
        );

        _logger.LogDebug(
            "User {UserId} wants to approve membership application {MembershipApplicationId}",
            approvedBy,
            applicationId
        );

        var application = await _membershipApplicationRepository.Get(applicationId);

        if (!await _authorizationService.CanApprove(approvedBy, application))
        {
            _logger.LogError(
                "User \"{UserId}\" is not authorized to approve membership application \"{MembershipApplicationId}\" for capability \"{CapabilityId}\".",
                approvedBy,
                application.Id,
                application.CapabilityId
            );

            throw new NotAuthorizedToApproveMembershipApplication(
                $"User \"{approvedBy}\" is not authorized to approve membership application \"{application.Id}\" for capability \"{application.CapabilityId}\"."
            );
        }

        application.Approve(approvedBy, _systemTime.Now);

        _logger.LogDebug(
            "Membership application {MembershipApplicationId} has received approval by {UserId} for capability {CapabilityId}",
            applicationId,
            approvedBy,
            application.CapabilityId
        );
    }

    [TransactionalBoundary, Outboxed]
    public async Task CancelExpiredMembershipApplications()
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(SubmitMembershipApplication),
            GetType().FullName
        );

        var expiredApplications = await _membershipApplicationRepository.FindExpiredApplications();

        // Please note: this violates the principle around "don't change multiple aggregates within the same transaction",
        // but this is a deliberate choice and serves to be an exception to the rule. The reasoning behind breaking
        // the principle is that it's the SAME type of aggregate (e.g. MembershipApplication) and they need to be
        // changed for the SAME business reason: they have expired.

        foreach (var application in expiredApplications)
        {
            _logger.LogInformation(
                "Membership application {MembershipApplicationId} by user {UserId} for capability {CapabilityId} has expired and is being cancelled.",
                application.Id,
                application.Applicant,
                application.CapabilityId
            );

            application.Cancel();
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task RemoveMembershipApplication(MembershipApplicationId applicationId)
    {
        await _membershipApplicationRepository.Remove(applicationId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task LeaveCapability(CapabilityId capabilityId, UserId userId)
    {
        using var _ = _logger.BeginScope(
            "{Action} on {ImplementationType}",
            nameof(LeaveCapability),
            GetType().FullName
        );

        _logger.LogDebug("User {UserId} wants to leave capapbility {CapabilityId}", userId, capabilityId);

        Membership? membership = await _membershipRepository.CancelWithCapabilityId(capabilityId, userId);
        if (membership != null)
        {
            membership.Cancel();

            _logger.LogDebug("User {UserId} has left capability {CapabilityId}", userId, capabilityId);
        }
        else
        {
            _logger.LogDebug(
                "User {UserId} could not leave capability {CapabilityId}, being the last member",
                userId,
                capabilityId
            );
        }
    }
    
    [TransactionalBoundary, Outboxed]
    public async Task AddUserToCapability(CapabilityId capabilityId, UserId userId)
    {
        _logger.LogInformation("User {userId} was directly added as a member of capability {capabilityId}", userId);
        await CreateAndAddMembership(capabilityId, userId);
    }
}

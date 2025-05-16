using Microsoft.Extensions.Azure;
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
    private readonly IInvitationRepository _invitationRepository;
    private readonly IMyCapabilitiesQuery _myCapabilitiesQuery;

    public MembershipApplicationService(
        ILogger<MembershipApplicationService> logger,
        ICapabilityRepository capabilityRepository,
        IMembershipRepository membershipRepository,
        IMembershipApplicationRepository membershipApplicationRepository,
        IAuthorizationService authorizationService,
        SystemTime systemTime,
        IMembershipQuery membershipQuery,
        IMembershipApplicationDomainService membershipApplicationDomainService,
        IInvitationRepository invitationRepository,
        IMyCapabilitiesQuery myCapabilitiesQuery
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
        _invitationRepository = invitationRepository;
        _myCapabilitiesQuery = myCapabilitiesQuery;
    }

    private async Task CreateAndAddMembership(CapabilityId capabilityId, UserId userId)
    {
        if (await _membershipRepository.IsAlreadyMember(capabilityId, userId))
        {
            throw new AlreadyHasActiveMembershipException(
                $"User \"{userId}\" is already member of \"{capabilityId}\"."
            );
        }
        var newMembership = Membership.CreateFor(
            capabilityId: capabilityId,
            userId: userId,
            createdAt: _systemTime.Now
        );

        await _membershipRepository.Add(newMembership);

        // Note: requires [TransactionalBoundary], which should be wrapped elsewhere
        var existingInvitations = await _invitationRepository.GetActiveInvitations(userId, capabilityId);
        foreach (var invitation in existingInvitations)
        {
            invitation.Cancel();
        }

        var existingApplications = await _membershipApplicationRepository.GetAllForUserAndCapability(
            userId,
            capabilityId
        );
        foreach (var application in existingApplications)
        {
            try
            {
                application.Cancel();
            }
            catch (MembershipAlreadyFinalisedException ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        _logger.LogInformation("User {UserId} has joined capability {CapabilityId}", userId, capabilityId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task AddCreatorAsInitialMember(CapabilityId capabilityId, UserId creatorId)
    {
        _logger.LogInformation(
            "Creator {CreatorId} is added as initial member to capability {CapabilityId}",
            creatorId,
            capabilityId
        );
        try
        {
            await CreateAndAddMembership(capabilityId, creatorId);
        }
        catch (AlreadyHasActiveMembershipException)
        {
            _logger.LogWarning(
                "Creator {CreatorId} is already a member of capability {CapabilityId}",
                creatorId,
                capabilityId
            );
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task AcceptApplication(MembershipApplicationId applicationId)
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

        try
        {
            await CreateAndAddMembership(membershipApplication.CapabilityId, membershipApplication.Applicant);
        }
        catch (AlreadyHasActiveMembershipException)
        {
            _logger.LogWarning(
                "User {UserId} is already a member of capability {CapabilityId}",
                membershipApplication.Applicant,
                membershipApplication.CapabilityId
            );
        }
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

        var membersOfCapability = await _membershipRepository.GetAllWithPredicate(
            x => x.CapabilityId == capabilityId
        );
        var membersOfCapabilityUserIds = membersOfCapability.Select(x => x.UserId.ToString()).ToList();

        var existingApplication = await _membershipApplicationRepository.FindPendingBy(capabilityId, userId);
        if (existingApplication != null)
        {
            // NOTE [jandr@2023-03-02]: should this be an EntityAlreadyExistsException instead??
            throw new PendingMembershipApplicationAlreadyExistsException(
                $"User \"{userId}\" already has a pending membership application for capability \"{capabilityId}\""
            );
        }

        var application = MembershipApplication.New(capabilityId, userId, _systemTime.Now, membersOfCapabilityUserIds);
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
            nameof(CancelExpiredMembershipApplications),
            GetType().FullName
        );

        var applications = await _membershipApplicationRepository.FindAllPending();

        // Please note: this violates the principle around "don't change multiple aggregates within the same transaction",
        // but this is a deliberate choice and serves to be an exception to the rule. The reasoning behind breaking
        // the principle is that it's the SAME type of aggregate (e.g. MembershipApplication) and they need to be
        // changed for the SAME business reason: they have expired.
        var now = DateTime.Now;
        foreach (var application in applications)
        {
            if (application.ExpiresOn < now)
            {
                _logger.LogDebug(
                    "Membership application to {capability} for user {user} has expired",
                    application.CapabilityId,
                    application.Applicant
                );
                await _membershipApplicationRepository.Remove(application);
            }
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
    public async Task JoinCapability(CapabilityId capabilityId, UserId userId)
    {
        _logger.LogInformation(
            "User {UserId} was directly added as a member of capability {CapabilityId}",
            userId,
            capabilityId
        );
        try
        {
            await CreateAndAddMembership(capabilityId, userId);
        }
        catch (AlreadyHasActiveMembershipException)
        {
            _logger.LogWarning("User {UserId} is already a member of capability {CapabilityId}", userId, capabilityId);
        }
    }

    public async Task<IEnumerable<MembershipApplication>> GetMembershipsApplicationsThatUserCanApprove(UserId userId)
    {
        var capabilities = await _myCapabilitiesQuery.FindBy(userId);
        var memberships = await _membershipApplicationRepository.GetAll();
        var membershipsThatUserCanApprove = memberships
            .ToList()
            .Where(x =>
                capabilities.Any(cap =>
                    cap.Id == x.CapabilityId && x.Status == MembershipApplicationStatusOptions.PendingApprovals
                )
            );

        return membershipsThatUserCanApprove.ToList();
    }

    public async Task<IEnumerable<MembershipApplication>> GetOutstandingMembershipsApplicationsForUser(UserId userId)
    {
        var memberships = await _membershipApplicationRepository.GetAll();
        var outstandingMembershipsApplicationsForUser = memberships.ToList().Where(x => x.Applicant == userId);

        return outstandingMembershipsApplicationsForUser.ToList();
    }
}

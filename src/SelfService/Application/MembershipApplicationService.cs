using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class MembershipApplicationService : IMembershipApplicationService
{
    private readonly ILogger<MembershipApplicationService> _logger;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipApplicationRepository _membershipApplicationRepository;
    private readonly SystemTime _systemTime;

    public MembershipApplicationService(ILogger<MembershipApplicationService> logger, ICapabilityRepository capabilityRepository, 
        IMembershipRepository membershipRepository, IMembershipApplicationRepository membershipApplicationRepository, 
        SystemTime systemTime)
    {
        _logger = logger;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _membershipApplicationRepository = membershipApplicationRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MembershipId> AcceptApplication(MembershipApplicationId applicationId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", 
            nameof(AcceptApplication), GetType().FullName);

        var membershipApplication = await _membershipApplicationRepository.Get(applicationId);
        if (!membershipApplication.IsFinalized)
        {
            _logger.LogError("Membership application {MembershipApplicationId} has status {MembershipApplicationStatus} and is not finalized", applicationId, membershipApplication.Status);
            throw new Exception($"Cannot start a new membership from a membership application (#{applicationId}) that is not finalized.");
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
        
        await _membershipRepository.Add(newMembership);

        _logger.LogInformation("User {UserId} has joined capability {CapabilityId}", 
            membershipApplication.Applicant, membershipApplication.CapabilityId);
        
        return newMembership.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MembershipApplicationId> SubmitMembershipApplication(CapabilityId capabilityId, UserId userId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType} for {CurrentUser}", 
            nameof(SubmitMembershipApplication), GetType().FullName, userId);

        // check if the user already has an "active" application for this capability
        
        var application = MembershipApplication.New(capabilityId, userId, _systemTime.Now);
        await _membershipApplicationRepository.Add(application);

        return application.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task TryFinalizeMembershipApplication(MembershipApplicationId applicationId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", 
            nameof(TryFinalizeMembershipApplication), GetType().FullName);

        var application = await _membershipApplicationRepository.Get(applicationId);
        var memberships = await _membershipRepository.FindBy(application.CapabilityId);
        
        var currentMemberCount = memberships.Count();
        var approvalCount = application.Approvals.Count();

        if (currentMemberCount == 1)
        {
            if (approvalCount == 1)
            {
                application.FinalizeApprovals();
            }
        }
        else if (currentMemberCount == 2)
        {
            if (approvalCount == 1)
            {
                application.FinalizeApprovals();
            }
        }
        else if (currentMemberCount > 2)
        {
            if (approvalCount == 2)
            {
                application.FinalizeApprovals();
            }
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task ApproveMembershipApplication(MembershipApplicationId applicationId, UserId approvedBy)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}",
            nameof(SubmitMembershipApplication), GetType().FullName);

        _logger.LogDebug("User {UserId} wants to approve membership application {MembershipApplicationId}", 
            approvedBy, applicationId);

        var application = await _membershipApplicationRepository.Get(applicationId);
        application.Approve(approvedBy, _systemTime.Now);

        _logger.LogDebug("Membership application {MembershipApplicationId} has received approval by {UserId} for capability {CapabilityId}",
            applicationId, approvedBy, application.CapabilityId);
    }

    [TransactionalBoundary, Outboxed]
    public async Task CancelExpiredMembershipApplications()
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", 
            nameof(SubmitMembershipApplication), GetType().FullName);

        var expiredApplications = await _membershipApplicationRepository.FindExpiredApplications();
        
        // Please note: this violates the principle around "don't change multiple aggregates within the same transaction",
        // but this is a deliberate choice and serves to be an exception to the rule. The reasoning behind breaking 
        // the principle is that it's the SAME type of aggregate (e.g. MembershipApplication) and they need to be
        // changed for the SAME business reason: they have expired.
        
        foreach (var application in expiredApplications)
        {
            _logger.LogInformation("Membership application {MembershipApplicationId} by user {UserId} for capability {CapabilityId} has expired and is being cancelled.", 
                application.Id, application.Applicant, application.CapabilityId);

            application.Cancel();
        }
    }
}
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
    public async Task<MembershipId> StartNewMembership(MembershipApplicationId applicationId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType}", 
            nameof(StartNewMembership), GetType().FullName);

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

        var application = MembershipApplication.New(capabilityId, userId, _systemTime.Now);
        await _membershipApplicationRepository.Add(application);

        return application.Id;
    }
}
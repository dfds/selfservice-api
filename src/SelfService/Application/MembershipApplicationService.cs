using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class MembershipApplicationService : IMembershipApplicationService
{
    private readonly ILogger<MembershipApplicationService> _logger;
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly SystemTime _systemTime;

    public MembershipApplicationService(ILogger<MembershipApplicationService> logger, ICapabilityRepository capabilityRepository, 
        IMembershipRepository membershipRepository, SystemTime systemTime)
    {
        _logger = logger;
        _capabilityRepository = capabilityRepository;
        _membershipRepository = membershipRepository;
        _systemTime = systemTime;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MembershipId> StartNewMembership(CapabilityId capabilityId, UserId userId)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType} for {CurrentUser}", 
            nameof(StartNewMembership), GetType().FullName, userId);

        var capabilityExists = await _capabilityRepository.Exists(capabilityId);
        if (!capabilityExists)
        {
            _logger.LogError("Could not find a capability with id {CapabilityId}", capabilityId);
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        // TODO [jandr@2023-02-22]: deal with user is unknown at this point!

        var newMembership = Membership.CreateFor(capabilityId, userId, _systemTime.Now);
        await _membershipRepository.Add(newMembership);

        return newMembership.Id;
    }
}
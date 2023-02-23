using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IMembershipApplicationService
{
    Task<MembershipId> StartNewMembership(CapabilityId capabilityId, UserId userId);
}
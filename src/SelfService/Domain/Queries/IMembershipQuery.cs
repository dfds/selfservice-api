using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IMembershipQuery
{
    Task<bool> HasActiveMembership(UserId userId, CapabilityId capabilityId);
    Task<bool> HasActiveMembershipApplication(UserId userId, CapabilityId capabilityId);
    Task<bool> HasMultipleMembers(CapabilityId capabilityId);
}

using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IMembershipQuery
{
    Task<bool> HasActiveMembership(UserId userId, CapabilityId capabilityId);
    Task<IEnumerable<Membership>> FindActiveBy(UserId userId);
}
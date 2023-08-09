using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface ICapabilityMembershipApplicationQuery
{
    Task<IEnumerable<MembershipApplication>> FindPendingBy(CapabilityId capabilityId);
}

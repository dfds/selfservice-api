using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface ICapabilityMembersQuery
{
    Task<IEnumerable<Member>> FindBy(CapabilityId capabilityId);
}
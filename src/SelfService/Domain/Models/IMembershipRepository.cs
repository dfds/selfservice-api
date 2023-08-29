namespace SelfService.Domain.Models;

public interface IMembershipRepository
{
    Task Add(Membership membership);
    Task<IEnumerable<Membership>> FindBy(CapabilityId capabilityId);
    Task<Membership?> CancelWithCapabilityId(CapabilityId capabilityId, UserId userId);
    Task<List<Membership>> CancelAllMembershipsWithUserId(UserId userId);
}

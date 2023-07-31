namespace SelfService.Domain.Models;

public interface IMembershipRepository
{
    Task Add(Membership membership);
    Task<IEnumerable<Membership>> FindBy(CapabilityId capabilityId);
    Task<Membership?> Cancel(CapabilityId capabilityId, UserId userId);
}

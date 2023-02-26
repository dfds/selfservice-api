namespace SelfService.Domain.Models;

public interface IMembershipRepository
{
    Task Add(Membership membership);
    Task<IEnumerable<Membership>> FindBy(CapabilityId capabilityId);
}
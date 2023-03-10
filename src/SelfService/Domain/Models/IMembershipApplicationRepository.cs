namespace SelfService.Domain.Models;

public interface IMembershipApplicationRepository
{
    Task Add(MembershipApplication application);
    Task<MembershipApplication> Get(MembershipApplicationId id);
    Task<IEnumerable<MembershipApplication>> FindExpiredApplications();
    Task<MembershipApplication?> FindPendingBy(CapabilityId capabilityId, UserId userId);
}
namespace SelfService.Domain.Models;

public interface IMembershipApplicationRepository
{
    Task Add(MembershipApplication application);
    Task<MembershipApplication> Get(MembershipApplicationId id);
    Task<IEnumerable<MembershipApplication>> GetAll();
    Task<IEnumerable<MembershipApplication>> FindAllPending();
    Task<IEnumerable<MembershipApplication>> FindExpiredApplications();
    Task<MembershipApplication?> FindPendingBy(CapabilityId capabilityId, UserId userId);
    Task<MembershipApplication?> FindBy(MembershipApplicationId id);
    Task Remove(MembershipApplicationId id);
    Task Remove(MembershipApplication application);

    Task<List<MembershipApplication>> RemoveAllWithUserId(UserId userId);

    Task<List<MembershipApplication>> GetAllForUserAndCapability(UserId userId, CapabilityId capabilityId);
}

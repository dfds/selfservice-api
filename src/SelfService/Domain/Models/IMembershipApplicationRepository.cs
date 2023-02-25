using SelfService.Application;

namespace SelfService.Domain.Models;

public interface IMembershipApplicationRepository
{
    Task Add(MembershipApplication application);
    Task<MembershipApplication> Get(MembershipApplicationId id);
}
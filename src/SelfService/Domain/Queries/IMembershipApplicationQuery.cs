using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IMembershipApplicationQuery
{
    Task<MembershipApplication?> FindById(MembershipApplicationId id);
}
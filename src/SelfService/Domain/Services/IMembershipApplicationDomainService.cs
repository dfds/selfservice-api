using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;
public interface IMembershipApplicationDomainService
{
    bool CanBeFinalized(MembershipApplication application);
    public Task<bool> CanApprove(UserId userId, MembershipApplication application);
}
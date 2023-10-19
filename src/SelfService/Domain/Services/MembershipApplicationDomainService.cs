using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class MembershipApplicationDomainService : IMembershipApplicationDomainService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipQuery _membershipQuery;

    public MembershipApplicationDomainService(
        IMembershipRepository membershipRepository,
        IMembershipQuery membershipQuery
    )
    {
        _membershipRepository = membershipRepository;
        _membershipQuery = membershipQuery;
    }

    public bool CanBeFinalized(MembershipApplication application)
    {
        var approvalCount = application.Approvals.Count();

        if (approvalCount >= 1)
        {
            return true;
        }

        return false;
    }
}

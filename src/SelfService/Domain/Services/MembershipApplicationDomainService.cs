using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Domain.Services;

public class MembershipApplicationDomainService
{
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipQuery _membershipQuery;

    public MembershipApplicationDomainService(IMembershipRepository membershipRepository, IMembershipQuery membershipQuery)
    {
        _membershipRepository = membershipRepository;
        _membershipQuery = membershipQuery;
    }

    public async Task<bool> CanBeFinalized(MembershipApplication application)
    {
        var approvalCount = application.Approvals.Count();

        if (approvalCount >= 1)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CanApprove(UserId userId, MembershipApplication application)
    {
        if (application.IsFinalized || application.IsCancelled)
        {
            return false;
        }

        if (userId == application.Applicant)
        {
            return false;
        }

        if (application.HasApproved(userId))
        {
            return false;
        }

        if (!await _membershipQuery.HasActiveMembership(userId, application.CapabilityId))
        {
            return false;
        }

        return true;
    }
}
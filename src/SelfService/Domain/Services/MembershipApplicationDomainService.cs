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
        var memberships = await _membershipRepository.FindBy(application.CapabilityId);

        var currentMemberCount = memberships.Count();
        var approvalCount = application.Approvals.Count();

        if (currentMemberCount == 1)
        {
            if (approvalCount == 1)
            {
                return true;
            }
        }
        else if (currentMemberCount == 2)
        {
            if (approvalCount >= 1)
            {
                return true;
            }
        }
        else if (currentMemberCount > 2)
        {
            if (approvalCount >= 2)
            {
                return true;
            }
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
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Tests.TestDoubles;

public class StubMembershipApplicationQuery : IMembershipApplicationQuery
{
    private readonly MembershipApplication? _membershipApplication;

    public StubMembershipApplicationQuery(MembershipApplication? membershipApplication = null)
    {
        _membershipApplication = membershipApplication;
    }

    public Task<MembershipApplication?> FindById(MembershipApplicationId id)
    {
        return Task.FromResult(_membershipApplication);
    }
}

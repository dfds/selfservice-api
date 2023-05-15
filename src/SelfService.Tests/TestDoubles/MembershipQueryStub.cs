using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Tests.TestDoubles;

public class MembershipQueryStub : IMembershipQuery
{
    private readonly bool _hasActiveMembership;
    private readonly bool _hasActiveMembershipApplication;
    private readonly bool _hasMultipleMembers;

    public MembershipQueryStub(bool hasActiveMembership = false, bool hasActiveMembershipApplication = false, bool hasMultipleMembers = false)
    {
        _hasActiveMembership = hasActiveMembership;
        _hasActiveMembershipApplication = hasActiveMembershipApplication;
        _hasMultipleMembers = hasMultipleMembers;
    }

    public Task<bool> HasActiveMembership(UserId userId, CapabilityId capabilityId)
        => Task.FromResult(_hasActiveMembership);

    public Task<bool> HasActiveMembershipApplication(UserId userId, CapabilityId capabilityId) 
        => Task.FromResult(_hasActiveMembershipApplication);

    public Task<bool> HasMultipleMembers(CapabilityId capabilityId)
        => Task.FromResult(_hasMultipleMembers);
}
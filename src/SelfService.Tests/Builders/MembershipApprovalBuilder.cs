using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class MembershipApprovalBuilder
{
    private Guid _id;
    private UserId _approvedBy;
    private DateTime _approvedAt;

    public MembershipApprovalBuilder()
    {
        _id = Guid.NewGuid();
        _approvedBy = UserId.Parse("foo");
        _approvedAt = new DateTime(2000, 1, 1);
    }

    public MembershipApprovalBuilder WithApprovedBy(UserId approvedBy)
    {
        _approvedBy = approvedBy;
        return this;
    }

    public MembershipApprovalBuilder WithMembershipApplicationId(MembershipApplicationId id)
    {
        _id = id;
        return this;
    }

    public MembershipApproval Build()
    {
        return new MembershipApproval(id: _id, approvedBy: _approvedBy, approvedAt: _approvedAt);
    }
}

using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class MembershipApplicationBuilder
{
    private MembershipApplicationId _id;
    private CapabilityId _capability;
    private UserId _applicant;
    private MembershipApplicationStatusOptions _status;
    private DateTime _submittedAt;
    private DateTime _expiresOn;
    private IEnumerable<MembershipApproval> _approvals;

    public MembershipApplicationBuilder()
    {
        _id = MembershipApplicationId.New();
        _capability = CapabilityId.Parse("foo");
        _applicant = UserId.Parse("bar");
        _status = MembershipApplicationStatusOptions.PendingApprovals;
        _submittedAt = new DateTime(2000, 1, 1);
        _expiresOn = _submittedAt.AddDays(1);
        _approvals = Enumerable.Empty<MembershipApproval>();
    }

    public MembershipApplicationBuilder WithApplicant(UserId applicant)
    {
        _applicant = applicant;
        return this;
    }

    public MembershipApplicationBuilder WithApplicant(string applicant)
    {
        _applicant = applicant;
        return this;
    }

    public MembershipApplicationBuilder WithApproval(Action<MembershipApprovalBuilder> modifier)
    {
        var builder = new MembershipApprovalBuilder();
        modifier(builder);
        return WithApprovals(builder.Build());
    }
    
    public MembershipApplicationBuilder WithApprovals(params MembershipApproval[] approvals)
    {
        _approvals = approvals;
        return this;
    }
    
    public MembershipApplicationBuilder WithApprovals(IEnumerable<MembershipApproval> approvals)
    {
        _approvals = approvals;
        return this;
    }
    
    public MembershipApplication Build()
    {
        return new MembershipApplication(
            id: _id,
            capabilityId: _capability,
            applicant: _applicant,
            approvals: new List<MembershipApproval>(_approvals),
            status: _status,
            submittedAt: _submittedAt,
            expiresOn: _expiresOn
        );
    }

    public static implicit operator MembershipApplication(MembershipApplicationBuilder builder)
        => builder.Build();

}
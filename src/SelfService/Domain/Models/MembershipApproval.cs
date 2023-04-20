namespace SelfService.Domain.Models;

public class MembershipApproval : Entity<Guid>
{
    private MembershipApproval()
    {
        
    }

    public MembershipApproval(Guid id, UserId approvedBy, DateTime approvedAt) : base(id)
    {
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
    }

    public UserId ApprovedBy { get; private set; }
    public DateTime ApprovedAt { get; private set; }

    public static MembershipApproval Register(UserId approvedBy, DateTime approvedAt)
    {
        return new MembershipApproval(Guid.NewGuid(), approvedBy, approvedAt);
    }
}
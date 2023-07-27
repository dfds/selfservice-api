using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class MembershipApplication : AggregateRoot<MembershipApplicationId>
{
    private MembershipApplication() { }
    
    private readonly IList<MembershipApproval> _approvals = new List<MembershipApproval>();
    private readonly CapabilityId _capabilityId = null!;
    private readonly UserId _applicant = null!;
    private MembershipApplicationStatusOptions _status;
    private readonly DateTime _submittedAt;
    private readonly DateTime _expiresOn;

    public MembershipApplication(MembershipApplicationId id, CapabilityId capabilityId, UserId applicant,
        IList<MembershipApproval> approvals, MembershipApplicationStatusOptions status, 
        DateTime submittedAt, DateTime expiresOn) : base(id)
    {
        _capabilityId = capabilityId;
        _applicant = applicant;
        _approvals = approvals;
        _status = status;
        _submittedAt = submittedAt;
        _expiresOn = expiresOn;
    }

    public CapabilityId CapabilityId => _capabilityId;
    public UserId Applicant => _applicant;
    public IEnumerable<MembershipApproval> Approvals => _approvals;
    public MembershipApplicationStatusOptions Status => _status;
    public DateTime SubmittedAt => _submittedAt;
    public DateTime ExpiresOn => _expiresOn;

    public bool IsFinalized => _status == MembershipApplicationStatusOptions.Finalized;
    public bool IsCancelled => _status == MembershipApplicationStatusOptions.Cancelled;

    public bool HasApproved(UserId userId) => _approvals.Any(x => x.ApprovedBy == userId);

    public void Approve(UserId approvedBy, DateTime approvedAt) 
    {
        if (approvedBy == Applicant)
        {
            throw new Exception($"User {approvedBy} cannot approve its own membership application {Id}");
        }

        if (_status == MembershipApplicationStatusOptions.Cancelled)
        {
            throw new Exception($"User {approvedBy} cannot approve a cancelled membership application {Id}");
        }
        
        if (_status == MembershipApplicationStatusOptions.Finalized)
        {
            // already finalized
            return;
        }
        
        if (HasApproved(approvedBy))
        {
            // already approved by THIS user
            return;
        }

        var approval = MembershipApproval.Register(approvedBy, approvedAt);
        _approvals.Add(approval);

        Raise(new MembershipApplicationHasReceivedAnApproval
        {
            MembershipApplicationId = Id.ToString()
        });
    }
    
    public void FinalizeApprovals()
    {
        if (_status == MembershipApplicationStatusOptions.Cancelled)
        {
            throw new Exception($"Cannot finalize a cancelled membership application {Id}");
        }
        
        if (_status == MembershipApplicationStatusOptions.Finalized)
        {
            return;
        }
        
        if (_status == MembershipApplicationStatusOptions.PendingApprovals)
        {
            _status = MembershipApplicationStatusOptions.Finalized;
            Raise(new MembershipApplicationHasBeenFinalized
            {
                MembershipApplicationId = Id.ToString()
            });
        }
    }

    public void Cancel()
    {
        if (_status == MembershipApplicationStatusOptions.Finalized)
        {
            throw new Exception($"Cannot cancel an already finalized membership application {Id}");
        }
        
        if (_status == MembershipApplicationStatusOptions.Cancelled)
        {
            return;
        }
        
        if (_status == MembershipApplicationStatusOptions.PendingApprovals)
        {
            _status = MembershipApplicationStatusOptions.Cancelled;
            Raise(new MembershipApplicationHasBeenCancelled
            {
                MembershipApplicationId = Id.ToString()
            });
        }
    }
    
    public static MembershipApplication New(CapabilityId capabilityId, UserId applicant, DateTime submittedAt)
    {
        var instance =  new MembershipApplication(
            id: MembershipApplicationId.New(),
            capabilityId: capabilityId,
            applicant: applicant,
            approvals: new List<MembershipApproval>(), 
            status: MembershipApplicationStatusOptions.PendingApprovals,
            submittedAt: submittedAt,
            expiresOn: submittedAt
                .AddDays(15)
                .Subtract(submittedAt.TimeOfDay)
        );
        
        instance.Raise(new NewMembershipApplicationHasBeenSubmitted
        {
            MembershipApplicationId = instance.Id.ToString()
        });

        return instance;
    }
}
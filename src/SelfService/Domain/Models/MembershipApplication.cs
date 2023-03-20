using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class MembershipApplication : AggregateRoot<MembershipApplicationId>
{
    private MembershipApplication() { }
    
    private readonly List<MembershipApproval> _approvals = new List<MembershipApproval>();
    private readonly CapabilityId _capabilityId = null!;
    private readonly UserId _applicant = null!;
    private MempershipApplicationStatusOptions _status;
    private readonly DateTime _submittedAt;
    private readonly DateTime _expiresOn;

    public MembershipApplication(MembershipApplicationId id, CapabilityId capabilityId, UserId applicant,
        IEnumerable<MembershipApproval> approvals, MempershipApplicationStatusOptions status, 
        DateTime submittedAt, DateTime expiresOn) : base(id)
    {
        _capabilityId = capabilityId;
        _applicant = applicant;
        _approvals.AddRange(approvals);
        _status = status;
        _submittedAt = submittedAt;
        _expiresOn = expiresOn;
    }

    public CapabilityId CapabilityId => _capabilityId;
    public UserId Applicant => _applicant;
    public IEnumerable<MembershipApproval> Approvals => _approvals;
    public MempershipApplicationStatusOptions Status => _status;
    public DateTime SubmittedAt => _submittedAt;
    public DateTime ExpiresOn => _expiresOn;

    public bool IsFinalized => _status == MempershipApplicationStatusOptions.Finalized;
    public bool IsCancelled => _status == MempershipApplicationStatusOptions.Cancelled;
    
    public void Approve(UserId approvedBy, DateTime approvedAt) 
    {
        if (approvedBy == Applicant)
        {
            throw new Exception($"User {approvedBy} cannot approve its own membership application {Id}");
        }

        if (_status == MempershipApplicationStatusOptions.Cancelled)
        {
            throw new Exception($"User {approvedBy} cannot approve a cancelled membership application {Id}");
        }
        
        if (_status == MempershipApplicationStatusOptions.Finalized)
        {
            // already finalized
            return;
        }
        
        if (_approvals.Any(x => x.ApprovedBy == approvedBy))
        {
            // already approved by THIS user
            return;
        }

        var approval = MembershipApproval.Register(approvedBy, approvedAt);
        _approvals.Add(approval);
        
        Raise(new MembershipApplicationHasRecievedAnApproval
        {
            MembershipApplicationId = Id.ToString()
        });
    }
    
    public void FinalizeApprovals()
    {
        if (_status == MempershipApplicationStatusOptions.Cancelled)
        {
            throw new Exception($"Cannot finalize a cancelled membership application {Id}");
        }
        
        if (_status == MempershipApplicationStatusOptions.Finalized)
        {
            return;
        }
        
        if (_status == MempershipApplicationStatusOptions.PendingApprovals)
        {
            _status = MempershipApplicationStatusOptions.Finalized;
            Raise(new MembershipApplicationHasBeenFinalized
            {
                MembershipApplicationId = Id.ToString()
            });
        }
    }

    public void Cancel()
    {
        if (_status == MempershipApplicationStatusOptions.Finalized)
        {
            throw new Exception($"Cannot cancel an already finalized membership application {Id}");
        }
        
        if (_status == MempershipApplicationStatusOptions.Cancelled)
        {
            return;
        }
        
        if (_status == MempershipApplicationStatusOptions.PendingApprovals)
        {
            _status = MempershipApplicationStatusOptions.Cancelled;
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
            approvals: Enumerable.Empty<MembershipApproval>(), 
            status: MempershipApplicationStatusOptions.PendingApprovals,
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
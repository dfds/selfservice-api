using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class Invitation : AggregateRoot<InvitationId>
{
    public UserId Invitee { get; private set; }
    public string TargetId { get; private set; }
    public InvitationTargetTypeOptions TargetType { get; private set; }
    public InvitationStatusOptions Status { get; private set; }
    public string CreatedBy { get; private set; }
    public string Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; private set; }

    public Invitation(
        InvitationId id,
        UserId invitee,
        string targetId,
        InvitationTargetTypeOptions targetType,
        string description,
        InvitationStatusOptions status,
        string createdBy,
        DateTime createdAt,
        DateTime modifiedAt
    )
        : base(id)
    {
        Invitee = invitee;
        TargetId = targetId;
        TargetType = targetType;
        Status = status;
        Description = description;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        ModifiedAt = modifiedAt;
    }

    public void Decline()
    {
        UpdateStatus(InvitationStatusOptions.Declined);
        RaiseEvent(new NewMembershipInvitationHasBeenDeclined { MembershipInvitationId = Id.ToString() });
    }

    public void Accept()
    {
        UpdateStatus(InvitationStatusOptions.Accepted);
        RaiseEvent(new NewMembershipInvitationHasBeenAccepted { MembershipInvitationId = Id.ToString() });
    }

    public void Cancel()
    {
        UpdateStatus(InvitationStatusOptions.Cancelled);
        RaiseEvent(new NewMembershipInvitationHasBeenCancelled { MembershipInvitationId = Id.ToString() });
    }

    public static Invitation New(
        string targetId,
        InvitationTargetTypeOptions targetType,
        UserId invitee,
        UserId createdBy,
        DateTime createdAt,
        string description
    )
    {
        var instance = new Invitation(
            id: InvitationId.New(),
            targetId: targetId.ToString(),
            targetType: targetType,
            invitee: invitee,
            description: description,
            status: InvitationStatusOptions.Active,
            createdBy: createdBy,
            createdAt: createdAt,
            modifiedAt: createdAt
        );

        instance.RaiseEvent(instance.CreateSubmittedEvent());

        return instance;
    }

    private void UpdateStatus(InvitationStatusOptions newStatus)
    {
        Status = newStatus;
        ModifiedAt = DateTime.UtcNow;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }

    private NewMembershipInvitationHasBeenSubmitted CreateSubmittedEvent() =>
        new NewMembershipInvitationHasBeenSubmitted
        {
            MembershipInvitationId = Id.ToString(),
            Invitee = Invitee.ToString(),
            TargetId = TargetId,
            TargetType = TargetType.ToString(),
            Description = Description,
            CreatedBy = CreatedBy,
            CreatedAt = CreatedAt,
        };
}

namespace SelfService.Domain.Models;

public class Invitation : Entity<InvitationId>
{
    public UserId Invitee { get; private set; }
    public string TargetId { get; private set; }
    public InvitationTargetTypeOptions TargetType { get; private set; }
    public InvitationStatusOptions Status { get; private set; }
    public string Description { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; private set; }

    public Invitation(
        InvitationId id,
        UserId invitee,
        string targetId,
        InvitationTargetTypeOptions targetType,
        InvitationStatusOptions status,
        string description,
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
        Status = InvitationStatusOptions.Declined;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Accept()
    {
        Status = InvitationStatusOptions.Accepted;
        ModifiedAt = DateTime.UtcNow;
    }
}

namespace SelfService.Domain.Models;

public class Invitation : Entity<InvitationId>
{
    public UserId Invitee { get; private set; }
    public Guid Target { get; private set; }
    public InvitationStatusOptions Status { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; private set; }

    public Invitation(
        InvitationId id,
        UserId invitee,
        Guid target,
        InvitationStatusOptions status,
        string createdBy,
        DateTime createdAt,
        DateTime modifiedAt
    )
        : base(id)
    {
        Invitee = invitee;
        Target = target;
        Status = status;
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

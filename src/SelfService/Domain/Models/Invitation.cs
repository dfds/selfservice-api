namespace SelfService.Domain.Models;

public class Invitation : Entity<InvitationId>
{
    public UserId Invitee { get; private set; }
    public Guid Target { get; private set; }
    public InvitationStatusOptions Status { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Invitation(
        InvitationId id,
        UserId invitee,
        Guid target,
        InvitationStatusOptions status,
        string createdBy,
        DateTime createdAt
    ) : base(id)
    {
        Invitee = invitee;
        Target = target;
        Status = status;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }
}

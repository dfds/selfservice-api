namespace SelfService.Domain.Models;

public class InvitationId : ValueObjectGuid<InvitationId>
{
    private InvitationId(Guid id)
        : base(id) { }
}

namespace SelfService.Domain.Models;

public class RbacGroupMemberId : ValueObjectGuid<RbacGroupMemberId>
{
    private RbacGroupMemberId(Guid id) : base(id)
    {
    }
}
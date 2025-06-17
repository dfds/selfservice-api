namespace SelfService.Domain.Models;

public class RbacRoleGrantId : ValueObjectGuid<RbacRoleGrantId>
{
    private RbacRoleGrantId(Guid id) : base(id)
    {
    }
}
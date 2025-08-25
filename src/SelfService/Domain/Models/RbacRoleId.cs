namespace SelfService.Domain.Models;

public class RbacRoleId : ValueObjectGuid<RbacRoleId>
{
    private RbacRoleId(Guid id)
        : base(id) { }
}

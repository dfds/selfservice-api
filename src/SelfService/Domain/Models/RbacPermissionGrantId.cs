namespace SelfService.Domain.Models;

public class RbacPermissionGrantId : ValueObjectGuid<RbacPermissionGrantId>
{
    private RbacPermissionGrantId(Guid id)
        : base(id) { }
}

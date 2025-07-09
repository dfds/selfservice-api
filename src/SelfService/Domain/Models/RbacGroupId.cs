namespace SelfService.Domain.Models;

public class RbacGroupId : ValueObjectGuid<RbacGroupId>
{
    private RbacGroupId(Guid id) : base(id)
    {
    }
}
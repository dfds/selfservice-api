using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacRoleGrant : AggregateRoot<RbacRoleGrantId>
{
    public RbacRoleId RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public AssignedEntityType AssignedEntityType { get; set; }
    public string AssignedEntityId { get; set; }
    public RbacAccessType Type { get; set; }
    public string? Resource { get; set; }

    public RbacRoleGrant(
        RbacRoleGrantId id,
        RbacRoleId roleId,
        DateTime createdAt,
        AssignedEntityType assignedEntityType,
        string assignedEntityId,
        RbacAccessType type,
        string resource
    )
        : base(id)
    {
        RoleId = roleId;
        CreatedAt = createdAt;
        AssignedEntityType = assignedEntityType;
        AssignedEntityId = assignedEntityId;
        Type = type;
        Resource = resource;
    }

    public static RbacRoleGrant New(
        RbacRoleId roleId,
        AssignedEntityType assignedEntityType,
        string assignedEntityId,
        RbacAccessType type,
        string resource
    )
    {
        var instance = new RbacRoleGrant(
            id: RbacRoleGrantId.New(),
            roleId: roleId,
            createdAt: DateTime.Now,
            assignedEntityType: assignedEntityType,
            assignedEntityId: assignedEntityId,
            type: type,
            resource: resource
        );

        // raise event
        instance.RaiseEvent(new RbacRoleGrantCreated { RbacRoleGrantId = instance.Id });
        return instance;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }

    // override ToString to print type and resource
    public override string ToString()
    {
        return $"RbacRoleGrant: {AssignedEntityId} -- {Type}, {Resource}";
    }
}

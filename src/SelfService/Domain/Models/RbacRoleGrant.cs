using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacRoleGrant : AggregateRoot<RbacRoleGrantId>
{
    public RbacRoleId RoleId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public AssignedEntityType AssignedEntityType { get; private set; }
    public string AssignedEntityId { get; private set; }
    public string Type { get; private set; }
    public string? Resource { get; private set; }

    public RbacRoleGrant(RbacRoleGrantId id, RbacRoleId roleId, DateTime createdAt, AssignedEntityType assignedEntityType, string assignedEntityId, string type, string resource) : base(id)
    {
        RoleId = roleId;
        CreatedAt = createdAt;
        AssignedEntityType = assignedEntityType;
        AssignedEntityId = assignedEntityId;
        Type = type;
        Resource = resource;
    }

    public static RbacRoleGrant New(RbacRoleId roleId, AssignedEntityType assignedEntityType, string assignedEntityId, string type, string resource)
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
        instance.RaiseEvent(new RbacRoleGrantCreated());
        return instance;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }
}
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacPermissionGrant : AggregateRoot<RbacPermissionGrantId>
{
    public DateTime CreatedAt { get; private set; }
    public AssignedEntityType AssignedEntityType { get; private set; }
    public string AssignedEntityId { get; private set; }
    public string Namespace { get; private set; }
    public string Permission { get; private set; }
    public string Type { get; set; }
    public string? Resource { get; set; }

    public RbacPermissionGrant(RbacPermissionGrantId id, DateTime createdAt, AssignedEntityType assignedEntityType, string assignedEntityId, string @namespace, string permission, string type, string resource) : base(id)
    {
        CreatedAt = createdAt;
        AssignedEntityType = assignedEntityType;
        AssignedEntityId = assignedEntityId;
        Namespace = @namespace;
        Permission = permission;
        Type = type;
        Resource = resource;
    }

    public static RbacPermissionGrant New(AssignedEntityType assignedEntityType, string assignedEntityId, string @namespace, string permission, string type, string resource)
    {
        var instance = new RbacPermissionGrant(
            id: RbacPermissionGrantId.New(),
            createdAt: DateTime.Now,
            assignedEntityType: assignedEntityType,
            assignedEntityId: assignedEntityId,
            @namespace: @namespace,
            permission: permission,
            type: type,
            resource: resource
            );

        // raise event
        instance.RaiseEvent(new RbacPermissionGrantCreated());
        return instance;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }
}
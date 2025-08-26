using SelfService.Application;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacPermissionGrant : AggregateRoot<RbacPermissionGrantId>
{
    public DateTime CreatedAt { get; set; }
    public AssignedEntityType AssignedEntityType { get; set; }
    public string AssignedEntityId { get; set; }
    public RbacNamespace Namespace { get; set; }
    public string Permission { get; set; }
    public RbacAccessType Type { get; set; }
    public string? Resource { get; set; }

    public RbacPermissionGrant(
        RbacPermissionGrantId id,
        DateTime createdAt,
        AssignedEntityType assignedEntityType,
        string assignedEntityId,
        RbacNamespace @namespace,
        string permission,
        RbacAccessType type,
        string resource
    )
        : base(id)
    {
        CreatedAt = createdAt;
        AssignedEntityType = assignedEntityType;
        AssignedEntityId = assignedEntityId;
        Namespace = @namespace;
        Permission = permission;
        Type = type;
        Resource = resource;
    }

    public static RbacPermissionGrant New(
        AssignedEntityType assignedEntityType,
        string assignedEntityId,
        RbacNamespace @namespace,
        string permission,
        RbacAccessType type,
        string resource
    )
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

    public override string ToString()
    {
        return $"RbacPermissionGrant: {Namespace}, {Type}, {Permission}, {Resource}";
    }
}

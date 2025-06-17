using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacRoleGrant : AggregateRoot<RbacRoleGrantId>
{
    public DateTime CreatedAt { get; private set; }
    public AssignedEntityType AssignedEntityType { get; private set; }
    public string AssignedEntityId { get; private set; }
    public string Name { get; private set; }
    public string Type { get; private set; }
    public string Resource { get; private set; }

    public RbacRoleGrant(RbacRoleGrantId id, DateTime createdAt, AssignedEntityType assignedEntityType, string assignedEntityId, string name, string type, string resource) : base(id)
    {
        CreatedAt = createdAt;
        AssignedEntityType = assignedEntityType;
        AssignedEntityId = assignedEntityId;
        Name = name;
        Type = type;
        Resource = resource;
    }

    public static RbacRoleGrant New(AssignedEntityType assignedEntityType, string assignedEntityId, string name, string type, string resource)
    {
        var instance = new RbacRoleGrant(
            id: RbacRoleGrantId.New(),
            createdAt: DateTime.Now,
            assignedEntityType: assignedEntityType,
            assignedEntityId: assignedEntityId,
            name: name,
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
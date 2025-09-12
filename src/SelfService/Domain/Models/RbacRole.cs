using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacRoleDTO
{
    public string Id { get; private set; }
    public string OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Type { get; private set; }

    public RbacRoleDTO(
        RbacRoleId id,
        String ownerId,
        DateTime createdAt,
        DateTime updatedAt,
        string name,
        string description,
        string type
    )
    {
        Id = id.ToString();
        OwnerId = ownerId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Name = name;
        Description = description;
        Type = type;
    }

    public static RbacRoleDTO FromRbacRole(RbacRole role)
    {
        return new RbacRoleDTO(
            id: role.Id,
            ownerId: role.OwnerId,
            createdAt: role.CreatedAt,
            updatedAt: role.UpdatedAt,
            name: role.Name,
            description: role.Description,
            type: role.Type.ToString()
        );
    }
}

public class RbacRole : AggregateRoot<RbacRoleId>
{
    public string OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public RbacAccessType Type { get; private set; }

    public RbacRole(
        RbacRoleId id,
        String ownerId,
        DateTime createdAt,
        DateTime updatedAt,
        string name,
        string description,
        RbacAccessType type
    )
        : base(id)
    {
        OwnerId = ownerId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Name = name;
        Description = description;
        Type = type;
    }

    public static RbacRole New(String ownerId, string name, string description, RbacAccessType type)
    {
        var instance = new RbacRole(
            id: RbacRoleId.New(),
            ownerId: ownerId,
            createdAt: DateTime.Now,
            updatedAt: DateTime.Now,
            name: name,
            description: description,
            type: type
        );

        // raise event
        instance.RaiseEvent(new RbacRoleCreated());
        return instance;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }
}

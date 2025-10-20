using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacGroup : AggregateRoot<RbacGroupId>
{
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public ICollection<RbacGroupMember> Members { get; private set; }

    public RbacGroup(RbacGroupId id, DateTime createdAt, DateTime updatedAt, string name, string description)
        : base(id)
    {
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        Name = name;
        Description = description;
        Members = new List<RbacGroupMember>();
    }

    public static RbacGroup New(string name, string description, ICollection<RbacGroupMember> members)
    {
        var instance = new RbacGroup(
            id: RbacGroupId.New(),
            createdAt: DateTime.Now,
            updatedAt: DateTime.Now,
            name: name,
            description: description
        );

        // raise event
        instance.RaiseEvent(new RbacGroupCreated());
        return instance;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }
}

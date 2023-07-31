using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class Capability : AggregateRoot<CapabilityId>
{
    public Capability(
        CapabilityId id,
        string name,
        string description,
        DateTime? deleted,
        DateTime createdAt,
        string createdBy
    )
        : base(id)
    {
        Name = name;
        Description = description;
        Deleted = deleted;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public static Capability CreateCapability(
        CapabilityId capabilityId,
        string name,
        string description,
        DateTime creationTime,
        string requestedBy
    )
    {
        var capability = new Capability(capabilityId, name, description, null, creationTime, requestedBy);
        capability.Raise(new CapabilityCreated(capabilityId, requestedBy));
        return capability;
    }

    public string Name { get; private set; }
    public string Description { get; set; }
    public DateTime? Deleted { get; set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }

    public override string ToString()
    {
        return Id.ToString();
    }
}

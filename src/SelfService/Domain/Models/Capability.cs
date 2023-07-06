using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class Capability : AggregateRoot<CapabilityId>
{
    protected Capability() { }

    public Capability(CapabilityId id, string name, string description, DateTime? deleted, DateTime createdAt, string createdBy, bool isCritical, bool containsPII) : base(id)
    {
        Name = name;
        Description = description;
        Deleted = deleted;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        IsCritical = isCritical;
        ContainsPII = containsPII;
    }

    public static Capability CreateCapability(CapabilityId capabilityId, string name, string description, DateTime creationTime, string requestedBy, bool isCritical, bool containsPII)
    {
        var capability = new Capability(capabilityId, name, description, null, creationTime, requestedBy, isCritical, containsPII);
        capability.Raise(new CapabilityCreated(capabilityId, requestedBy));
        return capability;
    }

    public string Name { get; private set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime? Deleted { get; set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public bool isCritical {get; private set;}
    public bool containsPII {get; private set;}

    public override string ToString()
    {
        return Id.ToString();
    }
}
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class Capability : AggregateRoot<CapabilityId>
{
    public Capability(CapabilityId id, string name, string description, DateTime createdAt, string createdBy)
        : base(id)
    {
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        Status = CapabilityStatusOptions.Active; // all new capabilities are active to begin with
        CreatedBy = createdBy;
        ModifiedAt = createdAt; // this will always be the same as CreatedAt for a new Capability
    }

    public static Capability CreateCapability(
        CapabilityId capabilityId,
        string name,
        string description,
        DateTime creationTime,
        string requestedBy
    )
    {
        var capability = new Capability(capabilityId, name, description, creationTime, requestedBy);
        capability.Raise(new CapabilityCreated(capabilityId, requestedBy));
        return capability;
    }

    public string Name { get; private set; }
    public string Description { get; set; }
    public CapabilityStatusOptions Status { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; private set; }
    public string CreatedBy { get; private set; }

    public override string ToString()
    {
        return Id.ToString();
    }

    public void RequestDeletion()
    {
        if (Status != CapabilityStatusOptions.Active)
        {
            throw new InvalidOperationException("Capability is not active");
        }

        Status = CapabilityStatusOptions.PendingDeletion;
        ModifiedAt = DateTime.UtcNow;
    }

    public void CancelDeletionRequest()
    {
        if (Status != CapabilityStatusOptions.PendingDeletion)
        {
            throw new InvalidOperationException("Capability is not pending deletion");
        }

        Status = CapabilityStatusOptions.Active;
        ModifiedAt = DateTime.UtcNow;
    }
}

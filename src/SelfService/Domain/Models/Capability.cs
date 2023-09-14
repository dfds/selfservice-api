using System.ComponentModel.DataAnnotations.Schema;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class Capability : AggregateRoot<CapabilityId>
{
    public Capability(
        CapabilityId id,
        string name,
        string description,
        DateTime createdAt,
        string createdBy,
        string jsonMetadata,
        int jsonMetadataSchemaVersion
    )
        : base(id)
    {
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        Status = CapabilityStatusOptions.Active; // all new capabilities are active to begin with
        CreatedBy = createdBy;
        ModifiedAt = createdAt; // this will always be the same as CreatedAt for a new Capability
        ModifiedBy = createdBy;
        JsonMetadata = jsonMetadata;
        JsonMetadataSchemaVersion = jsonMetadataSchemaVersion;
    }

    public static Capability CreateCapability(
        CapabilityId capabilityId,
        string name,
        string description,
        DateTime creationTime,
        string requestedBy,
        string jsonMetadata,
        int jsonMetadataSchemaVersion
    )
    {
        var capability = new Capability(
            capabilityId,
            name,
            description,
            creationTime,
            requestedBy,
            jsonMetadata,
            jsonMetadataSchemaVersion
        );
        capability.Raise(new CapabilityCreated(capabilityId, requestedBy));
        return capability;
    }

    public string Name { get; private set; }
    public string Description { get; set; }
    public CapabilityStatusOptions Status { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public string ModifiedBy { get; private set; }

    [Column(TypeName = "jsonb")]
    public string JsonMetadata { get; private set; }

    public int JsonMetadataSchemaVersion { get; private set; }

    public override string ToString()
    {
        return Id.ToString();
    }

    public void RequestDeletion(UserId userId)
    {
        if (Status != CapabilityStatusOptions.Active)
        {
            throw new InvalidOperationException("Capability is not active");
        }

        Status = CapabilityStatusOptions.PendingDeletion;
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = userId;
    }

    public void SetJsonMetadata(string jsonMetadata)
    {
        JsonMetadata = jsonMetadata;
    }

    public void SetModifiedDate(DateTime modifiedAt)
    {
        ModifiedAt = modifiedAt;
    }

    public void CancelDeletionRequest(UserId userId)
    {
        if (Status != CapabilityStatusOptions.PendingDeletion)
        {
            throw new InvalidOperationException("Capability is not pending deletion");
        }

        Status = CapabilityStatusOptions.Active;
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = userId;
    }

    public void MarkAsDeleted()
    {
        if (Status != CapabilityStatusOptions.PendingDeletion)
        {
            throw new InvalidOperationException("Capability is not pending deletion");
        }

        Status = CapabilityStatusOptions.Deleted;
        ModifiedAt = DateTime.UtcNow;
    }
}

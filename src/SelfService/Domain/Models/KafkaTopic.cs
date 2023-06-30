using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class KafkaTopic : AggregateRoot<KafkaTopicId>
{
    public KafkaTopic(KafkaTopicId id, KafkaClusterId kafkaClusterId, CapabilityId capabilityId, KafkaTopicName name, string description,
        KafkaTopicStatus status, KafkaTopicPartitions partitions, KafkaTopicRetention retention, DateTime createdAt, string createdBy, DateTime? modifiedAt, string? modifiedBy) : base(id)
    {
        KafkaClusterId = kafkaClusterId;
        CapabilityId = capabilityId;
        Name = name;
        Description = description;
        Status = status;
        Partitions = partitions;
        Retention = retention;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public KafkaClusterId KafkaClusterId { get; private set; }
    public KafkaTopicName Name { get; private set; }

    /// <summary>
    /// This is a computed value based on the name of the topic. It must begin with "pub."
    /// to be public, if not it is considered to be private. This value is not stored as
    /// part of this entity.
    /// </summary>
    public bool IsPublic => Name.ToString().StartsWith("pub.");

    /// <summary>
    /// This is a computed value based on the name of the topic. It must begin with "pub."
    /// to be public, if not it is considered to be private. This value is not stored as
    /// part of this entity.
    /// </summary>
    public bool IsPrivate => !IsPublic;

    public string Description { get; private set; }

    public void ChangeDescription(string newDescription, DateTime modifiedAt, string modifiedBy)
    {
        Description = newDescription;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }
        
    public KafkaTopicStatus Status { get; private set; }

    private void ChangeStatus(KafkaTopicStatus newStatus, DateTime modifiedAt, string modifiedBy)
    {
        Status = newStatus;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;

        // raise event??
    }

    public void RegisterAsInProgress(DateTime modifiedAt, string modifiedBy) 
        => ChangeStatus(KafkaTopicStatus.InProgress, modifiedAt, modifiedBy);

    public void RegisterAsProvisioned(DateTime modifiedAt, string modifiedBy) 
        => ChangeStatus(KafkaTopicStatus.Provisioned, modifiedAt, modifiedBy);

    public void Delete()
    {
        Raise(new KafkaTopicHasBeenDeleted
        {
            KafkaTopicId = Id.ToString()
        });
    }

    public KafkaTopicPartitions Partitions { get; private set; }
    public KafkaTopicRetention Retention { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
        
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    public static KafkaTopic RequestNew(KafkaClusterId kafkaClusterId, CapabilityId capabilityId, KafkaTopicName name, string description, 
        KafkaTopicPartitions partitions, KafkaTopicRetention retention, DateTime createdAt, string createdBy)
    {
        var instance = new KafkaTopic(
            id: KafkaTopicId.New(),
            kafkaClusterId: kafkaClusterId,
            capabilityId: capabilityId,
            name: name,
            description: description,
            status: KafkaTopicStatus.Requested,
            partitions: partitions,
            retention: retention,
            createdAt: createdAt,
            createdBy: createdBy,
            modifiedAt: null,
            modifiedBy: null
        );

        instance.Raise(new NewKafkaTopicHasBeenRequested
        {
            KafkaTopicId = instance.Id.ToString(),
            KafkaTopicName = instance.Name.ToString(),
            KafkaClusterId = kafkaClusterId.ToString(),
            CapabilityId = capabilityId.ToString(),
            Partitions = instance.Partitions.ToValue(),
            Retention = instance.Retention.ToString()
        });

        return instance;
    }
}
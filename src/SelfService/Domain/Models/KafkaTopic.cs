using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class KafkaTopic : AggregateRoot<KafkaTopicId>
{
    public KafkaTopic(KafkaTopicId id, KafkaClusterId kafkaClusterId, CapabilityId capabilityId, KafkaTopicName name, string description,
        KafkaTopicStatusType status, uint partitions, long retention, DateTime createdAt, string createdBy, DateTime? modifiedAt, string? modifiedBy) : base(id)
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

    public string Description { get; private set; }

    public void ChangeDescription(string newDescription, DateTime modifiedAt, string modifiedBy)
    {
        Description = newDescription;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }
        
    public KafkaTopicStatusType Status { get; private set; }

    private void ChangeStatus(KafkaTopicStatusType newStatus, DateTime modifiedAt, string modifiedBy)
    {
        Status = newStatus;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;

        // raise event??
    }

    public void RegisterAsInProgress(DateTime modifiedAt, string modifiedBy) 
        => ChangeStatus(KafkaTopicStatusType.InProgress, modifiedAt, modifiedBy);

    public void RegisterAsProvisioned(DateTime modifiedAt, string modifiedBy) 
        => ChangeStatus(KafkaTopicStatusType.Provisioned, modifiedAt, modifiedBy);

    public uint Partitions { get; private set; }
    public long Retention { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
        
    public DateTime? ModifiedAt { get; private set; }
    public string? ModifiedBy { get; private set; }

    public static KafkaTopic RequestNew(KafkaClusterId kafkaClusterId, CapabilityId capabilityId, KafkaTopicName name, string description, 
        uint partitions, long retention, DateTime createdAt, string createdBy)
    {
        var instance = new KafkaTopic(
            id: KafkaTopicId.New(),
            kafkaClusterId: kafkaClusterId,
            capabilityId: capabilityId,
            name: name,
            description: description,
            status: KafkaTopicStatusType.Requested,
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
            KafkaClusterId = kafkaClusterId.ToString(),
            CapabilityId = capabilityId.ToString(),
        });

        return instance;
    }
}
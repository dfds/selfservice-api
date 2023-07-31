using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class KafkaClusterAccess : AggregateRoot<Guid>
{
    public KafkaClusterAccess(
        Guid id,
        CapabilityId capabilityId,
        KafkaClusterId kafkaClusterId,
        DateTime createdAt,
        string createdBy
    )
        : base(id)
    {
        CapabilityId = capabilityId;
        KafkaClusterId = kafkaClusterId;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public CapabilityId CapabilityId { get; private set; }
    public KafkaClusterId KafkaClusterId { get; private set; }
    public DateTime? GrantedAt { get; private set; }
    public bool IsAccessGranted => GrantedAt.HasValue;

    public void RegisterAsGranted(DateTime timestamp)
    {
        if (IsAccessGranted)
        {
            return;
        }

        GrantedAt = timestamp;
    }

    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }

    public static KafkaClusterAccess RequestAccess(
        CapabilityId capabilityId,
        KafkaClusterId kafkaClusterId,
        DateTime createdAt,
        string createdBy
    )
    {
        var instance = new KafkaClusterAccess(
            id: Guid.NewGuid(),
            capabilityId: capabilityId,
            kafkaClusterId: kafkaClusterId,
            createdAt: createdAt,
            createdBy: createdBy
        );

        instance.Raise(
            new KafkaClusterAccessRequested
            {
                CapabilityId = capabilityId.ToString(),
                KafkaClusterId = kafkaClusterId.ToString(),
            }
        );

        return instance;
    }
}

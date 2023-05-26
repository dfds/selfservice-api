namespace SelfService.Domain.Models;

public class KafkaClusterAccess : AggregateRoot<Guid>
{
    public KafkaClusterAccess(Guid id, CapabilityId capabilityId, KafkaClusterId kafkaClusterId, DateTime createdAt, string createdBy) : base(id)
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
    
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
}
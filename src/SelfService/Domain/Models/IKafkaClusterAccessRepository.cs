namespace SelfService.Domain.Models;

public interface IKafkaClusterAccessRepository
{
    Task Add(KafkaClusterAccess kafkaClusterAccess);

    Task<KafkaClusterAccess?> FindBy(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);
}
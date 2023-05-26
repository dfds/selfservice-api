namespace SelfService.Domain.Models;

public interface IKafkaClusterAccessRepository
{
    Task<KafkaClusterAccess?> FindBy(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);
}
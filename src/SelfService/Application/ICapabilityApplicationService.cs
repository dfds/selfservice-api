using SelfService.Domain.Models;

namespace SelfService.Application;

public interface ICapabilityApplicationService
{
    Task<CapabilityId> CreateNewCapability(CapabilityId capabilityId, string name, string description,
        string requestedBy);

    // NOTE [jandr@2023-03-27]: should this be moved to a more topic-centric use-case (e.g. topic application service)?
    Task<KafkaTopicId> RequestNewTopic(CapabilityId capabilityId, KafkaClusterId kafkaClusterId, KafkaTopicName name,
        string description, KafkaTopicPartitions partitions, KafkaTopicRetention retention, string requestedBy);
    Task RequestKafkaClusterAccess(CapabilityId capabilityId, KafkaClusterId kafkaClusterId, UserId requestedBy);
    Task RegisterKafkaClusterAccessGranted(CapabilityId capabilityId, KafkaClusterId kafkaClusterId);
}
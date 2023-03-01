using SelfService.Domain.Models;

namespace SelfService.Application;

public interface ICapabilityApplicationService
{
    Task<KafkaTopicId> RequestNewTopic(CapabilityId capabilityId, KafkaClusterId kafkaClusterId, KafkaTopicName name, 
        string description, KafkaTopicPartitions partitions, KafkaTopicRetention retention, string requestedBy);
}
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface ICapabilityApplicationService
{
    Task<KafkaTopicId> RequestNewTopic(CapabilityId capabilityId, KafkaClusterId kafkaClusterId, KafkaTopicName name, 
        string description, uint partitions, long retention, string requestedBy);
}
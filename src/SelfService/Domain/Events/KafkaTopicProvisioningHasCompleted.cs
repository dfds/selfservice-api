using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class KafkaTopicProvisioningHasCompleted : IDomainEvent
{
    public string? ClusterId { get; set; }
    public string? TopicId { get; set; }
    public string? TopicName { get; set; }
}

using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewKafkaTopicHasBeenRequested : IDomainEvent
{
    public string? KafkaTopicId { get; set; }
    public string? KafkaClusterId { get; set; }
    public string? CapabilityId { get; set; }
}
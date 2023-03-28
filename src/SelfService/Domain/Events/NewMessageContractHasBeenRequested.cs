using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMessageContractHasBeenRequested : IDomainEvent
{
    public string? MessageContractId { get; set; }
    public string? KafkaTopicId { get; set; }
    public string? KafkaTopicName { get; set; }
    public string? KafkaClusterId { get; set; }
    public string? CapabilityId { get; set; }
    public string? MessageType { get; set; }
    public string? Schema { get; set; }
    public string? Description { get; set; }
}
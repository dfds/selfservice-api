using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewMessageContractHasBeenProvisioned: IDomainEvent
{
    public string? MessageContractId { get; set; }
    public string? KafkaTopicId { get; set; }
    public string? MessageType { get; set; }
}
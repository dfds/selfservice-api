using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class KafkaTopicHasBeenDeleted: IDomainEvent
{
    public string? KafkaTopicId { get; set; }
}
using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class KafkaClusterAccessGranted : IDomainEvent
{
    public string? CapabilityId { get; set; }
    public string? KafkaClusterId { get; set; }
}

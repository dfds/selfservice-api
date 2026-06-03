using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class CapabilityReadyForDeletion : IDomainEvent
{
    public string? CapabilityId { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? RequestedAt { get; set; }

    public const string EventType = "capability-ready-for-deletion";
}

using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class CapabilityDeletionRequestSubmitted : IDomainEvent
{
    public string? CapabilityId { get; set; }
    public List<string>? Members { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }

    public const string EventType = "capability-deletion-request-submitted";
};

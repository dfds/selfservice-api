using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class CapabilityCreated : IDomainEvent
{
    public string CapabilityId { get; }
    public string RequestedBy { get; }

    public CapabilityCreated(string capabilityId, string requestedBy)
    {
        CapabilityId = capabilityId;
        RequestedBy = requestedBy;
    }

    public const string EventType = "capability-created";
}

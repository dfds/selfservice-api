using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class NewPortalVisitRegistered : IDomainEvent
{
    public string? PortalVisitId { get; set; }
    public string? VisitedBy { get; set; }
    public string? VisitedAt { get; set; }
}
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Events;

public class EventUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public EventType? Type { get; set; }
}

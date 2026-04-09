using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Events;

public class EventUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? EventDate { get; set; }
    public EventType? Type { get; set; }
    public List<EventAttachmentDto>? Attachments { get; set; }
}

public class EventAttachmentDto
{
    // If ID is provided, this is an existing attachment to keep
    // If ID is null, this is a new attachment to create
    public EventAttachmentId? Id { get; set; }
    public string? Url { get; set; }
    public EventAttachmentType Type { get; set; }
    public string? Description { get; set; }
}

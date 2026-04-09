using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Events;

public class EventAttachmentUpdateRequest
{
    public string? Url { get; set; }
    public EventAttachmentType? Type { get; set; }
    public string? Description { get; set; }
}

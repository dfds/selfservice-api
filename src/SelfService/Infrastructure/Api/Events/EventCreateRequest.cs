using System.ComponentModel.DataAnnotations;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Events;

public class EventCreateRequest
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Description { get; set; }

    /// <summary>
    /// ISO 8601 UTC instant including time-of-day, e.g. <c>2026-06-15T14:30:00Z</c>.
    /// Clients should convert the user's locally-entered date and time to UTC before sending.
    /// </summary>
    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public EventType Type { get; set; }

    public List<EventAttachmentCreateDto>? Attachments { get; set; }
}

public class EventAttachmentCreateDto
{
    [Required]
    public string? Url { get; set; }

    [Required]
    public EventAttachmentType Type { get; set; }

    public string? Description { get; set; }
}

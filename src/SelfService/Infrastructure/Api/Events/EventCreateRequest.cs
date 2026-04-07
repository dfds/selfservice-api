using System.ComponentModel.DataAnnotations;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Events;

public class EventCreateRequest
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Description { get; set; }

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

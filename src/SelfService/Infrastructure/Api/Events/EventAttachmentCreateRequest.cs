using System.ComponentModel.DataAnnotations;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Events;

public class EventAttachmentCreateRequest
{
    [Required]
    public string? Url { get; set; }

    [Required]
    public EventAttachmentType Type { get; set; }

    public string? Description { get; set; }
}

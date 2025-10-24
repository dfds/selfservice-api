using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Demos;

public class DemoCreateRequest
{
    [Required]
    public string? Title { get; set; }

    [Required]
    public string? Description { get; set; }

    [Required]
    public string? Uri { get; set; }

    [Required]
    public string? Tags { get; set; }

    [Required]
    public DateTime RecordingDate { get; set; }

    [Required]
    public bool IsActive { get; set; }
}

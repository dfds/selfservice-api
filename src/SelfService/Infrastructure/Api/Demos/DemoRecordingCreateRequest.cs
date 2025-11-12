using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Demos;

public class DemoRecordingCreateRequest
{
    [Required]
    public string? Title { get; set; }
    [Required]
    public string? Description { get; set; }
    [Required]
    public string? Url { get; set; }
    [Required]
    public DateTime RecordingDate { get; set; }
}

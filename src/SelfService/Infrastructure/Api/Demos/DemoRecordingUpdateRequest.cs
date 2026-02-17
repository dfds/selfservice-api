using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Demos;

public class DemoRecordingUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? RecordingUrl { get; set; }
    public string? SlidesUrl { get; set; }
    public DateTime RecordingDate { get; set; }
}

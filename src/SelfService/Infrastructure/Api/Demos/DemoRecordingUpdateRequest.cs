using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Demos;

public class DemoRecordingUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public DateTime RecordingDate { get; set; }

}

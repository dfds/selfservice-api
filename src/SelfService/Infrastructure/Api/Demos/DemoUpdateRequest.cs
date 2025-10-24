using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Demos;

public class DemoUpdateRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Uri { get; set; }

    public string? Tags { get; set; }

    public DateTime RecordingDate { get; set; }

    public bool IsActive { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewCapabilityRequest
{
    [Required]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? JsonMetadata { get; set; }
    public List<string>? Invitees { get; set; } = new();
}

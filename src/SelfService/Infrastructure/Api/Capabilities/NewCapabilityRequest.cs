using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class NewCapabilityRequest
{
    [Required]
    public string? Name { get; set; }
    public string? Description { get; set; }

    public int JsonSchemaVersion { get; set; }

    public string? JsonMetadata { get; set; }
}

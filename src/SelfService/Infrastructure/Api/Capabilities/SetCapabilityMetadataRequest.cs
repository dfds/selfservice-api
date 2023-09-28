using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SetCapabilityMetadataRequest
{
    [Required]
    public string JsonMetadata { get; set; } = "";
}

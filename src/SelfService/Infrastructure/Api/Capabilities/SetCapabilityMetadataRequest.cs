using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;

namespace SelfService.Infrastructure.Api.Capabilities;

public class SetCapabilityMetadataRequest
{
    [Required]
    public JsonObject? JsonMetadata { get; set; } = null;
}

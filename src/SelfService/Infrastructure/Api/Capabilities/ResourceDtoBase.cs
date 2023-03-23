using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

[Obsolete]
public abstract class ResourceDtoBase
{
    [Obsolete]
    [JsonPropertyName("_links")]
    public Dictionary<string, ResourceLink> Links { get; set; } = new();
}
using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityListApiResource
{
    public CapabilityDetailsApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityListLinks Links { get; set; } = new();

    public class CapabilityListLinks
    {
        public ResourceLink Self { get; set; }
    }
}
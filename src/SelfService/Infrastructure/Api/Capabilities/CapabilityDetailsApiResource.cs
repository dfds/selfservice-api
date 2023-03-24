using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityDetailsApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public CapabilityDetailsLinks Links { get; set; } = new();

    public class CapabilityDetailsLinks
    {
        public ResourceLink Self { get; set; } = new();
        public ResourceLink Members { get; set; } = new();
        public ResourceLink Topics { get; set; } = new();
    }
}
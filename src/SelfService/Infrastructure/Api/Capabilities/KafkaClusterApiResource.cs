using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterLinks Links { get; set; } = new();

    public class KafkaClusterLinks
    {
        public ResourceLink Self { get; set; } = new();
    }

}
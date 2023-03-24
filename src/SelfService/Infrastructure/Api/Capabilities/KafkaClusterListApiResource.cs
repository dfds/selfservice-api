using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterListApiResource
{
    public KafkaClusterApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterListLinks Links { get; set; } = new();

    public class KafkaClusterListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}
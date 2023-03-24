using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaTopicListApiResource
{
    public KafkaTopicApiResource[] Items { get; set; }

    [JsonPropertyName("_embedded")]
    public KafkaTopicListEmbeddedResources Embedded { get; set; } = new();

    [JsonPropertyName("_links")]
    public KafkaTopicListLinks Links { get; set; } = new();

    public class KafkaTopicListEmbeddedResources
    {
        public KafkaClusterListApiResource KafkaClusters { get; set; }
    }

    public class KafkaTopicListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}
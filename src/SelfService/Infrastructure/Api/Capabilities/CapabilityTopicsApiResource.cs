using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityTopicsApiResource
{
    public KafkaTopicApiResource[] Items { get; set; }

    [JsonPropertyName("_embedded")]
    public CapabilityTopicsEmbeddedResources Embedded { get; set; } = new();

    [JsonPropertyName("_links")]
    public CapabilityTopicsLinks Links { get; set; } = new();


    public class CapabilityTopicsEmbeddedResources
    {
        public KafkaClusterListApiResource KafkaClusters { get; set; }
    }

    public class CapabilityTopicsLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}
using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaTopicApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CapabilityId { get; set; }
    public string KafkaClusterId { get; set; }
    public uint Partitions { get; set; }
    public string Retention { get; set; }
    public string Status { get; set; }

    [JsonPropertyName("_links")]
    public KafkaTopicLinks Links { get; set; }

    public class KafkaTopicLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink MessageContracts { get; set; }
    }
}

public class KafkaTopicListApiResource
{
    public KafkaTopicApiResource[] Items { get; set; }

    [JsonPropertyName("_embedded")]
    public KafkaTopicListEmbeddedResources Embedded { get; set; }

    [JsonPropertyName("_links")]
    public KafkaTopicListLinks Links { get; set; }

    public class KafkaTopicListEmbeddedResources
    {
        public KafkaClusterListApiResource KafkaClusters { get; set; }
    }

    public class KafkaTopicListLinks
    {
        public ResourceLink Self { get; set; }
    }
}
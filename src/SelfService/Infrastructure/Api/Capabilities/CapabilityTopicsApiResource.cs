using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityTopicsApiResource
{
    public CapabilityClusterTopicsApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterListLinks Links { get; set; } = new();

    public class KafkaClusterListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}

public class CapabilityClusterTopicsApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    public KafkaTopicApiResource[] Topics { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterLinks Links { get; set; } = new();

    public class KafkaClusterLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}
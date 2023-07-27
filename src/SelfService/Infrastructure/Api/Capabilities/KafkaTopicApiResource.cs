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
    public KafkaTopicLinks Links { get; set; } = new();

    public class KafkaTopicLinks
    {
        public ResourceLink Self { get; set; } = new();
        public ResourceLink MessageContracts { get; set; } = new();
        public ResourceLink Consumers { get; set; } = new();
        public ResourceActionLink? UpdateDescription { get; set; }
    }
}
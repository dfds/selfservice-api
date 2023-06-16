using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterAccessApiResource
{
    public string? BootstrapServers { get; set; }
    public string? SchemaRegistryUrl { get; set; }
    
    [JsonPropertyName("_links")]
    public KafkaClusterAccess Links { get; set; } = new();

    public class KafkaClusterAccess
    {
        public ResourceLink Self { get; set; } = new();
    }
}
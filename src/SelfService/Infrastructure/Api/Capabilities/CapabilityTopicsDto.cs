using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class CapabilityTopicsDto : ResourceDtoBase
{
    public KafkaTopicDto[] Items { get; set; }

    [JsonPropertyName("_embedded")]
    public Dictionary<string, object> Embedded { get; set; } = new();
}
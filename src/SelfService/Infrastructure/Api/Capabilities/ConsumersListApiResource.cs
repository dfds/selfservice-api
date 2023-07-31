using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class ConsumersListApiResource
{
    public string[] Items { get; set; }

    [JsonPropertyName("_links")]
    public ConsumerListLinks Links { get; set; } = new();

    public class ConsumerListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}

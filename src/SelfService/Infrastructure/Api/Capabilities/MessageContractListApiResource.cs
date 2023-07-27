using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MessageContractListApiResource
{
    public MessageContractApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MessageContractListLinks Links { get; set; } = new();

    public class MessageContractListLinks
    {
        public ResourceLink Self { get; set; } = new();
    }
}
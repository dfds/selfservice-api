using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MessageContractListApiResource
{
    public MessageContractDto[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MessageContractListLinks Links { get; set; }

    public class MessageContractListLinks
    {
        public ResourceLink Self { get; set; }
    }
}
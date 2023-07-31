using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MessageContractListApiResource
{
    public MessageContractApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public MessageContractListLinks Links { get; set; }

    public class MessageContractListLinks
    {
        public ResourceLink Self { get; set; }

        public MessageContractListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public MessageContractListApiResource(MessageContractApiResource[] items, MessageContractListLinks links)
    {
        Items = items;
        Links = links;
    }
}

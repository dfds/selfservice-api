using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class ConsumersListApiResource
{
    public string[] Items { get; set; }

    [JsonPropertyName("_links")]
    public ConsumersListLinks Links { get; set; }

    public class ConsumersListLinks
    {
        public ResourceLink Self { get; set; }

        public ConsumersListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public ConsumersListApiResource(string[] items, ConsumersListLinks links)
    {
        Items = items;
        Links = links;
    }
}

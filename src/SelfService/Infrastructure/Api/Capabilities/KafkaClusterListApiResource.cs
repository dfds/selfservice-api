using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterListApiResource
{
    public KafkaClusterApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterListLinks Links { get; set; }

    public class KafkaClusterListLinks
    {
        public ResourceLink Self { get; set; }

        public KafkaClusterListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public KafkaClusterListApiResource(KafkaClusterApiResource[] items, KafkaClusterListLinks links)
    {
        Items = items;
        Links = links;
    }
}

using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaTopicListApiResource
{
    public KafkaTopicApiResource[] Items { get; set; } = Array.Empty<KafkaTopicApiResource>();

    [JsonPropertyName("_embedded")]
    public KafkaTopicListEmbeddedResources Embedded { get; set; }

    [JsonPropertyName("_links")]
    public KafkaTopicListLinks Links { get; set; }

    public class KafkaTopicListEmbeddedResources
    {
        public KafkaClusterListApiResource KafkaClusters { get; set; }

        public KafkaTopicListEmbeddedResources(KafkaClusterListApiResource kafkaClusters)
        {
            KafkaClusters = kafkaClusters;
        }
    }

    public class KafkaTopicListLinks
    {
        public ResourceLink Self { get; set; }

        public KafkaTopicListLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public KafkaTopicListApiResource(
        KafkaTopicApiResource[] items,
        KafkaTopicListEmbeddedResources embedded,
        KafkaTopicListLinks links
    )
    {
        Items = items;
        Embedded = embedded;
        Links = links;
    }
}

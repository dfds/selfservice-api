using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterAccessListApiResource
{
    public KafkaClusterAccessListItemApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterAccessList Links { get; set; } = new();

    public class KafkaClusterAccessList
    {
        public ResourceLink Self { get; set; } = new();
    }
}

public class KafkaClusterAccessListItemApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterAccessListItem Links { get; set; } = new();

    public class KafkaClusterAccessListItem
    {
        public ResourceLink Topics { get; set; } = new();
        public ResourceLink Access { get; set; } = new();
        public ResourceLink RequestAccess { get; set; } = new();
        public ResourceLink CreateTopic { get; set; } = new();
    }
}

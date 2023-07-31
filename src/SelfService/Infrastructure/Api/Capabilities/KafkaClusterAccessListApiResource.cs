using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterAccessListApiResource
{
    public KafkaClusterAccessListItemApiResource[] Items { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterAccessList Links { get; set; }

    public class KafkaClusterAccessList
    {
        public ResourceLink Self { get; set; }

        public KafkaClusterAccessList(ResourceLink self)
        {
            Self = self;
        }
    }

    public KafkaClusterAccessListApiResource(KafkaClusterAccessListItemApiResource[] items, KafkaClusterAccessList links)
    {
        Items = items;
        Links = links;
    }
}

public class KafkaClusterAccessListItemApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterAccessListItem Links { get; set; }

    public class KafkaClusterAccessListItem
    {
        public ResourceLink Topics { get; set; }
        public ResourceLink Access { get; set; }
        public ResourceLink RequestAccess { get; set; }
        public ResourceLink CreateTopic { get; set; }

        public KafkaClusterAccessListItem(ResourceLink topics, ResourceLink access, ResourceLink requestAccess, ResourceLink createTopic)
        {
            Topics = topics;
            Access = access;
            RequestAccess = requestAccess;
            CreateTopic = createTopic;
        }
    }

    public KafkaClusterAccessListItemApiResource(string id, string name, string description, KafkaClusterAccessListItem links)
    {
        Id = id;
        Name = name;
        Description = description;
        Links = links;
    }
}

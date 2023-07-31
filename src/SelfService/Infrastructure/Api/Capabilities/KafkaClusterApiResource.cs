using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [JsonPropertyName("_links")]
    public KafkaClusterLinks Links { get; set; }

    public class KafkaClusterLinks
    {
        public ResourceLink Self { get; set; }

        public KafkaClusterLinks(ResourceLink self)
        {
            Self = self;
        }
    }

    public KafkaClusterApiResource(string id, string name, string description, KafkaClusterLinks links)
    {
        Id = id;
        Name = name;
        Description = description;
        Links = links;
    }
}

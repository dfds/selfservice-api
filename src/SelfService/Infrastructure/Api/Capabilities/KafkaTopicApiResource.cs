using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaTopicApiResource
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CapabilityId { get; set; }
    public string KafkaClusterId { get; set; }
    public uint Partitions { get; set; }
    public string Retention { get; set; }
    public string Status { get; set; }

    [JsonPropertyName("_links")]
    public KafkaTopicLinks Links { get; set; }

    public class KafkaTopicLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink MessageContracts { get; set; }
        public ResourceLink Consumers { get; set; }
        public ResourceActionLink? UpdateDescription { get; set; }

        public KafkaTopicLinks(ResourceLink self, ResourceLink messageContracts, ResourceLink consumers, ResourceActionLink? updateDescription)
        {
            Self = self;
            MessageContracts = messageContracts;
            Consumers = consumers;
            UpdateDescription = updateDescription;
        }
    }

    public KafkaTopicApiResource(string id, string name, string description, string capabilityId, string kafkaClusterId, uint partitions, string retention, string status, KafkaTopicLinks links)
    {
        Id = id;
        Name = name;
        Description = description;
        CapabilityId = capabilityId;
        KafkaClusterId = kafkaClusterId;
        Partitions = partitions;
        Retention = retention;
        Status = status;
        Links = links;
    }
}

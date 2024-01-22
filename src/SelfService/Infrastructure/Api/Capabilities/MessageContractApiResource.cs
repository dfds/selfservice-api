using System.Text.Json.Serialization;

namespace SelfService.Infrastructure.Api.Capabilities;

public class MessageContractApiResource
{
    public string Id { get; set; }
    public string MessageType { get; set; }
    public string Description { get; set; }
    public string KafkaTopicId { get; set; }
    public string Schema { get; set; }
    public string Example { get; set; }
    public string Status { get; set; }

    public int SchemaVersion { get; set; }

    [JsonPropertyName("_links")]
    public MessageContractLinks Links { get; set; }

    public class MessageContractLinks
    {
        public ResourceLink Self { get; set; }
        public ResourceLink Retry { get; set; }

        public MessageContractLinks(ResourceLink self, ResourceLink retry)
        {
            Self = self;
            Retry = retry;
        }
    }

    public MessageContractApiResource(
        string id,
        string messageType,
        string description,
        string kafkaTopicId,
        string schema,
        string example,
        string status,
        int schemaVersion,
        MessageContractLinks links
    )
    {
        Id = id;
        MessageType = messageType;
        Description = description;
        KafkaTopicId = kafkaTopicId;
        Schema = schema;
        Example = example;
        Status = status;
        SchemaVersion = schemaVersion;
        Links = links;
    }
}

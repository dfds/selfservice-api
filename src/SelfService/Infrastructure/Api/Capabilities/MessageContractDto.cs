namespace SelfService.Infrastructure.Api.Capabilities;

public class MessageContractDto : ResourceDtoBase
{
    public string Id { get; set; }
    public string MessageType { get; set; }
    public string Description { get; set; }
    public string KafkaTopicId { get; set; }
    public string Schema { get; set; }
    public string Example { get; set; }
    public string Status { get; set; }
}
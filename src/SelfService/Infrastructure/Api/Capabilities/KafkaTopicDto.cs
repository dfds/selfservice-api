namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaTopicDto : ResourceDtoBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CapabilityId { get; set; }
    public string KafkaClusterId { get; set; }
    public uint Partitions { get; set; }
    public string Retention { get; set; }
    public string Status { get; set; }
}
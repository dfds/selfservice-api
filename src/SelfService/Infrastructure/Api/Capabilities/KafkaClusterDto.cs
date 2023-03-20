namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterDto : ResourceDtoBase
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
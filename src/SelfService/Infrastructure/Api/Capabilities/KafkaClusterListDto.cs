namespace SelfService.Infrastructure.Api.Capabilities;

public class KafkaClusterListDto : ResourceDtoBase
{
    public KafkaClusterDto[] Items { get; set; }
}
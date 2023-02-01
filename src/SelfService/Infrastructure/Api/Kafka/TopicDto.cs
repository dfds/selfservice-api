using SelfService.Legacy.Models;

namespace SelfService.Infrastructure.Api.Kafka;

public record TopicDto(Guid Id, string? Name, string? Description, Guid CapabilityId, Guid KafkaClusterId, int Partitions, string? Status)
{
    public Dictionary<string, object>? Configurations { get; set; }

    public static TopicDto CreateFrom(Topic topic)
    {
        var topicDto = new TopicDto(
            topic.Id,
            topic.Name,
            topic.Description,
            topic.CapabilityId,
            topic.KafkaClusterId,
            topic.Partitions,
            topic.Status)
        {
            Configurations = topic.Configurations
        };

        return topicDto;
    }
}
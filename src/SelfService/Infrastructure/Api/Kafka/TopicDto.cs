using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Kafka;

public record TopicDto(Guid Id, string? Name, string? Description, string CapabilityId, Guid KafkaClusterId, int Partitions, long Retention, string? Status)
{

    public static TopicDto CreateFrom(KafkaTopic topic)
    {
        return new TopicDto(
            topic.Id,
            topic.Name,
            topic.Description,
            topic.CapabilityId,
            topic.KafkaClusterId,
            topic.Partitions,
            topic.Retention,
            topic.Status);
    }
}
using SelfService.Domain.Models;

namespace SelfService.Domain.Queries;

public interface IKafkaTopicQuery
{
    Task<IEnumerable<KafkaTopic>> Query(KafkaTopicQueryParams queryParams, UserId userId);
}

public class KafkaTopicQueryParams
{
    public string? CapabilityId { get; set; }
    public string? ClusterId { get; set; }
    public bool? IncludePrivate { get; set; }
}
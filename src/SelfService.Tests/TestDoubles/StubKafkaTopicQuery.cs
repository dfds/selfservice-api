using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Tests.TestDoubles;

public class StubKafkaTopicQuery : IKafkaTopicQuery
{
    private readonly KafkaTopic[] _kafkaTopics;

    public StubKafkaTopicQuery(params KafkaTopic[] kafkaTopics)
    {
        _kafkaTopics = kafkaTopics;
    }

    public Task<IEnumerable<KafkaTopic>> Query(KafkaTopicQueryParams queryParams, UserId userId)
    {
        return Task.FromResult<IEnumerable<KafkaTopic>>(_kafkaTopics);
    }
}
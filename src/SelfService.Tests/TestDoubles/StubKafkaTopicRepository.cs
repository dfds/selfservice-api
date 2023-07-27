using SelfService.Domain.Models;

namespace SelfService.Tests.TestDoubles;

public class StubKafkaTopicRepository : IKafkaTopicRepository
{
    private readonly KafkaTopic[] _topics;

    public StubKafkaTopicRepository(params KafkaTopic[] topics)
    {
        _topics = topics;
    }

    public Task Add(KafkaTopic topic)
    {
        throw new NotImplementedException();
    }

    public Task<KafkaTopic> Get(KafkaTopicId id)
    {
        return Task.FromResult(_topics.Single());
    }

    public Task<IEnumerable<KafkaTopic>> GetAllPublic()
    {
        return Task.FromResult(_topics.AsEnumerable());
    }

    public Task<bool> Exists(KafkaTopicName name, KafkaClusterId clusterId)
    {
        throw new NotImplementedException();
    }

    public Task<KafkaTopic?> FindBy(KafkaTopicId id)
    {
        return Task.FromResult(_topics.SingleOrDefault());
    }

    public Task<IEnumerable<KafkaTopic>> FindBy(CapabilityId capabilityId)
    {
        return Task.FromResult(_topics.AsEnumerable());
    }

    public Task<KafkaTopic?> FindBy(KafkaTopicName name, KafkaClusterId clusterId)
    {
        throw new NotImplementedException();
    }

    public Task Delete(KafkaTopic topic)
    {
        throw new NotImplementedException();
    }
}
namespace SelfService.Domain.Models;

public interface IKafkaTopicRepository
{
    Task Add(KafkaTopic topic);
    Task<KafkaTopic> Get(KafkaTopicId id);
    Task<IEnumerable<KafkaTopic>> GetAllPublic();

    Task<bool> Exists(KafkaTopicName name, KafkaClusterId clusterId);
    Task<KafkaTopic?> FindBy(KafkaTopicId id);
    Task<IEnumerable<KafkaTopic>> FindBy(CapabilityId capabilityId);
    Task<KafkaTopic?> FindBy(KafkaTopicName name, KafkaClusterId clusterId);

    Task Delete(KafkaTopic topic);
}
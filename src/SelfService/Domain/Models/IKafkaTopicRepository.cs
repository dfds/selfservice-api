namespace SelfService.Domain.Models;

public interface IKafkaTopicRepository
{
    Task Add(KafkaTopic topic);
    Task<bool> Exists(KafkaTopicName name);
    Task<KafkaTopic> Get(KafkaTopicId id);
}
namespace SelfService.Domain.Models;

public interface IKafkaTopicRepository
{
    Task Add(KafkaTopic topic);
    Task<bool> Exists(KafkaTopicName name);
    Task<KafkaTopic> Get(KafkaTopicId id);
    Task<KafkaTopic?> FindBy(KafkaTopicId id);
    
    Task<IEnumerable<KafkaTopic>> GetAllPublic();
    Task<IEnumerable<KafkaTopic>> FindBy(CapabilityId capabilityId);
}
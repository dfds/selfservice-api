namespace SelfService.Domain.Models;

public interface IMessageContractRepository
{
    Task Add(MessageContract messageContract);
    void Update(MessageContract messageContract);
    Task<MessageContract> Get(MessageContractId id);
    Task<MessageContract?> FindBy(MessageContractId id);
    Task<IEnumerable<MessageContract>> FindBy(KafkaTopicId topicId);
    Task<MessageContract?> GetLatestSchema(KafkaTopicId topicId, MessageType messageType);

    Task<bool> Exists(KafkaTopicId topicId, MessageType messageType);
    Task Delete(MessageContract messageContract);
}

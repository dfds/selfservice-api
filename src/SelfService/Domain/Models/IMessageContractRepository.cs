namespace SelfService.Domain.Models;

public interface IMessageContractRepository
{
    Task Add(MessageContract messageContract);
    Task<MessageContract> Get(MessageContractId id);
    Task<MessageContract?> FindBy(MessageContractId id);
    Task<IEnumerable<MessageContract>> FindBy(KafkaTopicId topicId);
}
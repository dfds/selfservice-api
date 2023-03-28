using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IKafkaTopicApplicationService
{
    Task<MessageContractId> RequestNewMessageContract(KafkaTopicId kafkaTopicId, MessageType messageType, string description,
        MessageContractExample example, MessageContractSchema schema, string requestedBy);

    Task RegisterMessageContractAsProvisioned(MessageContractId messageContractId, string changedBy);
}
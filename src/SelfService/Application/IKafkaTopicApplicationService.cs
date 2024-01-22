using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IKafkaTopicApplicationService
{
    Task<MessageContractId> RequestNewMessageContract(
        KafkaTopicId kafkaTopicId,
        MessageType messageType,
        string description,
        MessageContractExample example,
        MessageContractSchema schema,
        string requestedBy,
        bool enforceSchemaEnvelope
    );

    Task RetryRequestNewMessageContract(
        KafkaTopicId kafkaTopicId,
        MessageContractId messageContractId,
        string requestedBy
    );

    Task RegisterMessageContractAsProvisioned(MessageContractId messageContractId, string changedBy);
    Task RegisterMessageContractAsFailed(MessageContractId messageContractId, string changedBy);

    Task RegisterKafkaTopicAsInProgress(KafkaTopicId kafkaTopicId, string changedBy);
    Task RegisterKafkaTopicAsProvisioned(KafkaTopicId kafkaTopicId, string changedBy);

    Task ChangeKafkaTopicDescription(KafkaTopicId kafkaTopicId, string newDescription, string changedBy);
    Task DeleteKafkaTopic(KafkaTopicId kafkaTopicId, string requestedBy);
    Task DeleteAssociatedMessageContracts(KafkaTopicId kafkaTopicId, string requestedBy);

    Task ValidateRequestForCreatingNewContract(
        KafkaTopicId kafkaTopicId,
        MessageType messageType,
        MessageContractSchema newSchema
    );
}

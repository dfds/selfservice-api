using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class KafkaTopicApplicationService : IKafkaTopicApplicationService
{
    private readonly ILogger<KafkaTopicApplicationService> _logger;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly SystemTime _systemTime;
    private readonly IKafkaTopicRepository _kafkaTopicRepository;

    public KafkaTopicApplicationService(ILogger<KafkaTopicApplicationService> logger, IMessageContractRepository messageContractRepository, 
        SystemTime systemTime, IKafkaTopicRepository kafkaTopicRepository)
    {
        _logger = logger;
        _messageContractRepository = messageContractRepository;
        _systemTime = systemTime;
        _kafkaTopicRepository = kafkaTopicRepository;
    }

    [TransactionalBoundary, Outboxed]
    public async Task<MessageContractId> RequestNewMessageContract(KafkaTopicId kafkaTopicId, MessageType messageType, string description, 
        MessageContractExample example, MessageContractSchema schema, string requestedBy)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType} requested by {RequestedBy}",
            nameof(RequestNewMessageContract), GetType().FullName, requestedBy);

        if (await _messageContractRepository.Exists(kafkaTopicId, messageType))
        {
            _logger.LogError("Cannot request new message contract {MessageType} for topic {KafkaTopicId} because it already exists.", 
                messageType, kafkaTopicId);

            throw new EntityAlreadyExistsException($"Message contract \"{messageType}\" already exists on topic \"{kafkaTopicId}\".");
        }

        var topic = await _kafkaTopicRepository.Get(kafkaTopicId);

        var messageContract = MessageContract.RequestNew(
            kafkaTopicId: kafkaTopicId,
            messageType: messageType,
            description: description,
            kafkaTopicName: topic.Name,
            kafkaClusterId: topic.KafkaClusterId,
            capabilityId: topic.CapabilityId,
            example: example,
            schema: schema,
            createdAt: _systemTime.Now,
            createdBy: requestedBy
        );

        await _messageContractRepository.Add(messageContract);

        _logger.LogInformation("New message contract {MessageContractId} for message type {MessageType} has been added to topic {KafkaTopicName}", 
            messageContract.Id, messageContract.MessageType, topic.Name);

        return messageContract.Id;
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterMessageContractAsProvisioned(MessageContractId messageContractId, string changedBy)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsProvisioned), GetType().FullName, changedBy);

        var messageContract = await _messageContractRepository.Get(messageContractId);
        messageContract.RegisterAsProvisioned(_systemTime.Now, changedBy);

        _logger.LogInformation("Message contract {MessageContractId} for message type {MessageType} has been provisioned.", 
            messageContract.Id, messageContract.MessageType);
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterKafkaTopicAsInProgress(KafkaTopicId kafkaTopicId, string changedBy)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsProvisioned), GetType().FullName, changedBy);

        var kafkaTopic = await _kafkaTopicRepository.FindBy(kafkaTopicId);
        if (kafkaTopic is not null)
        {
            kafkaTopic.RegisterAsInProgress(_systemTime.Now, changedBy);
            _logger.LogInformation("Kafka topic provisioning for \"{KafkaTopicName}\" is now in progress", kafkaTopic.Name);
        }
    }

    [TransactionalBoundary, Outboxed]
    public async Task RegisterKafkaTopicAsProvisioned(KafkaTopicId kafkaTopicId, string changedBy)
    {
        using var _ = _logger.BeginScope("{Action} on {ImplementationType} invoked by {ChangedBy}",
            nameof(RegisterMessageContractAsProvisioned), GetType().FullName, changedBy);

        var kafkaTopic = await _kafkaTopicRepository.FindBy(kafkaTopicId);

        if (kafkaTopic is not null)
        {
            kafkaTopic.RegisterAsProvisioned(_systemTime.Now, changedBy);
            _logger.LogInformation("Kafka topic {KafkaTopicName} has now been provisioned", kafkaTopic.Name);
        }
    }
}
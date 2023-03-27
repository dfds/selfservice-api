using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class KafkaTopicApplicationService : IKafkaTopicApplicationService
{
    private readonly ILogger<KafkaTopicApplicationService> _logger;
    private readonly IMessageContractRepository _messageContractRepository;
    private readonly SystemTime _systemTime;

    public KafkaTopicApplicationService(ILogger<KafkaTopicApplicationService> logger, IMessageContractRepository messageContractRepository, SystemTime systemTime)
    {
        _logger = logger;
        _messageContractRepository = messageContractRepository;
        _systemTime = systemTime;
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

        var messageContract = MessageContract.RequestNew(
            kafkaTopicId: kafkaTopicId,
            messageType: messageType,
            description: description,
            example: example,
            schema: schema,
            createdAt: _systemTime.Now,
            createdBy: requestedBy
        );

        await _messageContractRepository.Add(messageContract);

        return messageContract.Id;
    }
}
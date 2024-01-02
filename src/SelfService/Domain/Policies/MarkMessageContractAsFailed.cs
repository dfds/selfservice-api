using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Domain.Policies;

public class MarkMessageContractAsFailed : IMessageHandler<SchemaRegistrationFailed>
{
    private readonly ILogger<MarkMessageContractAsFailed> _logger;
    private readonly IKafkaTopicApplicationService _kafkaTopicApplicationService;
    private readonly IMessageContractRepository _messageContractRepository;

    public MarkMessageContractAsFailed(
        ILogger<MarkMessageContractAsFailed> logger,
        IKafkaTopicApplicationService kafkaTopicApplicationService,
        IMessageContractRepository messageContractRepository
    )
    {
        _logger = logger;
        _kafkaTopicApplicationService = kafkaTopicApplicationService;
        _messageContractRepository = messageContractRepository;
    }

    public async Task Handle(SchemaRegistrationFailed message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope(
            "Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType,
            GetType().Name,
            context.CorrelationId,
            context.CausationId
        );
        if (!MessageContractId.TryParse(message.MessageContractId, out var messageContractId))
        {
            _logger.LogError(
                "Cannot mark message contract as failed because message contract id \"{MessageContractId}\" is not valid - skipping message {MessageId}",
                message.MessageContractId,
                context.MessageId
            );

            return;
        }

        var storedContract = await _messageContractRepository.Get(messageContractId);

        _logger.LogError(
            "failed to register message contract {MessageContractId} for topic {TopicId} with reason: {MessageContractFailedReason}",
            message.MessageContractId,
            storedContract.KafkaTopicId,
            message.Reason
        );

        try
        {
            var changedBy = string.Join("/", "SYSTEM", GetType().FullName);
            await _kafkaTopicApplicationService.RegisterMessageContractAsFailed(
                messageContractId: messageContractId,
                changedBy: changedBy
            );
        }
        catch (EntityNotFoundException<MessageContract> err)
        {
            _logger.LogError(
                err,
                "Message contract \"{MessageContractId}\" could not be found and cannot be marked as failed - skipping!",
                messageContractId
            );
        }
    }
}

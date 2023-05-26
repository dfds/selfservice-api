using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Domain.Policies;

public class DeleteAssociatedMessageContracts : IMessageHandler<KafkaTopicHasBeenDeleted>
{
    private readonly ILogger<DeleteAssociatedMessageContracts> _logger;
    private readonly IKafkaTopicApplicationService _kafkaTopicApplicationService;

    public DeleteAssociatedMessageContracts(ILogger<DeleteAssociatedMessageContracts> logger, IKafkaTopicApplicationService kafkaTopicApplicationService)
    {
        _logger = logger;
        _kafkaTopicApplicationService = kafkaTopicApplicationService;
    }

    public async Task Handle(KafkaTopicHasBeenDeleted message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope("Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType, GetType().Name, context.CorrelationId, context.CausationId);

        if (!KafkaTopicId.TryParse(message.KafkaTopicId, out var topicId))
        {
            _logger.LogWarning("Unable to parse kafka topic id from {KafkaTopicId} - skipping message {MessageId}/{MessageType}", 
                message.KafkaTopicId, context.MessageId, context.MessageType);

            return;
        }

        await _kafkaTopicApplicationService.DeleteAssociatedMessageContracts(topicId, nameof(DeleteAssociatedMessageContracts));
    }
}
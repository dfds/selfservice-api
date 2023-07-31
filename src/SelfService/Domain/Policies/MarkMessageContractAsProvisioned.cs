using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Domain.Policies;

public class MarkMessageContractAsProvisioned : IMessageHandler<SchemaRegistered>
{
    private readonly ILogger<MarkMessageContractAsProvisioned> _logger;
    private readonly IKafkaTopicApplicationService _kafkaTopicApplicationService;

    public MarkMessageContractAsProvisioned(
        ILogger<MarkMessageContractAsProvisioned> logger,
        IKafkaTopicApplicationService kafkaTopicApplicationService
    )
    {
        _logger = logger;
        _kafkaTopicApplicationService = kafkaTopicApplicationService;
    }

    public async Task Handle(SchemaRegistered message, MessageHandlerContext context)
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
            _logger.LogWarning(
                "Cannot mark message contract as provisioned because message contract id \"{MessageContractId}\" is not valid - skipping message {MessageId}",
                message.MessageContractId,
                context.MessageId
            );

            return;
        }

        try
        {
            await _kafkaTopicApplicationService.RegisterMessageContractAsProvisioned(
                messageContractId: messageContractId,
                changedBy: string.Join("/", "SYSTEM", GetType().FullName)
            );
        }
        catch (EntityNotFoundException<MessageContract> err)
        {
            _logger.LogWarning(
                err,
                "Message contract \"{MessageContractId}\" could not be found and cannot be marked as provisioned - skipping!",
                messageContractId
            );
        }
    }
}

public class SchemaRegistered : IDomainEvent
{
    public string? MessageContractId { get; set; }
}

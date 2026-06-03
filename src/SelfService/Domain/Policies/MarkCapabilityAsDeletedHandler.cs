using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Domain.Policies;

public class MarkCapabilityAsDeletedHandler : IMessageHandler<CapabilityReadyForDeletion>
{
    private readonly ILogger<MarkCapabilityAsDeletedHandler> _logger;
    private readonly ICapabilityApplicationService _capabilityApplicationService;

    public MarkCapabilityAsDeletedHandler(
        ILogger<MarkCapabilityAsDeletedHandler> logger,
        ICapabilityApplicationService capabilityApplicationService
    )
    {
        _logger = logger;
        _capabilityApplicationService = capabilityApplicationService;
    }

    public async Task Handle(CapabilityReadyForDeletion message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope(
            "Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType,
            GetType().Name,
            context.CorrelationId,
            context.CausationId
        );

        if (!CapabilityId.TryParse(message.CapabilityId, out var capabilityId))
        {
            _logger.LogWarning(
                "Cannot mark capability as deleted because capability id \"{CapabilityId}\" is not valid - skipping message {MessageId}",
                message.CapabilityId,
                context.MessageId
            );
            return;
        }

        _logger.LogDebug("Marking capability {CapabilityId} as deleted", capabilityId);
        await _capabilityApplicationService.MarkCapabilityAsDeleted(capabilityId);
    }
}

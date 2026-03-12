using Dafda.Consuming;
using SelfService.Application;
using SelfService.Domain.Events;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Messaging;

public class EmailCampaignDeliveryHandler : IMessageHandler<EmailCampaignDeliveryCompleted>
{
    private readonly ILogger<EmailCampaignDeliveryHandler> _logger;
    private readonly IEmailCampaignApplicationService _emailCampaignApplicationService;

    public EmailCampaignDeliveryHandler(
        ILogger<EmailCampaignDeliveryHandler> logger,
        IEmailCampaignApplicationService emailCampaignApplicationService
    )
    {
        _logger = logger;
        _emailCampaignApplicationService = emailCampaignApplicationService;
    }

    public async Task Handle(EmailCampaignDeliveryCompleted message, MessageHandlerContext context)
    {
        using var _ = _logger.BeginScope(
            "Handling {MessageType} on {ImplementationType} with {CorrelationId} and {CausationId}",
            context.MessageType,
            GetType().Name,
            context.CorrelationId,
            context.CausationId
        );

        if (!EmailCampaignRecipientLogId.TryParse(message.RecipientLogId, out var recipientLogId))
        {
            _logger.LogError(
                "Invalid RecipientLogId {RecipientLogId} in delivery status event - skipping",
                message.RecipientLogId
            );
            return;
        }

        if (!EmailCampaignRecipientStatus.TryParse(message.Status, out var status) ||
            (status != EmailCampaignRecipientStatus.Sent && status != EmailCampaignRecipientStatus.Failed))
        {
            _logger.LogWarning(
                "Unknown delivery status {Status} for recipient log {RecipientLogId} - skipping",
                message.Status,
                message.RecipientLogId
            );
            return;
        }

        await _emailCampaignApplicationService.UpdateDeliveryStatus(recipientLogId, status, message.ErrorMessage);

        if (status == EmailCampaignRecipientStatus.Sent)
            _logger.LogInformation(
                "Marked recipient log {RecipientLogId} as Sent for campaign {CampaignId}",
                message.RecipientLogId,
                message.CampaignId
            );
        else
            _logger.LogWarning(
                "Marked recipient log {RecipientLogId} as Failed for campaign {CampaignId}: {ErrorMessage}",
                message.RecipientLogId,
                message.CampaignId,
                message.ErrorMessage
            );
    }
}

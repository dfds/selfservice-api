using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class EmailCampaignSendRequested : IDomainEvent
{
    public const string EventType = "email-campaign-send-requested";

    public string? CampaignId { get; set; }
    public string? RecipientLogId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? Subject { get; set; }
    public string? HtmlBody { get; set; }
}

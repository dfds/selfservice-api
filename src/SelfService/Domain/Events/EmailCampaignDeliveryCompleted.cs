using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class EmailCampaignDeliveryCompleted : IDomainEvent
{
    public const string EventType = "email-send-delivery-completed";

    public string? EmailSendId { get; set; }
    public string? RecipientLogId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}

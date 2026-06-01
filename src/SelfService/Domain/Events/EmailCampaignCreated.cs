using SelfService.Domain.Models;

namespace SelfService.Domain.Events;

public class EmailCampaignCreated : IDomainEvent
{
    public const string EventType = "email-campaign-created";

    public string? CampaignId { get; set; }
    public string? Name { get; set; }
    public string? CreatedBy { get; set; }
}

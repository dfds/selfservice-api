namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class EmailCampaignRecipientLogApiResource
{
    public string Id { get; set; } = "";
    public string EmailCampaignId { get; set; } = "";
    public string? CapabilityId { get; set; }
    public string? CapabilityName { get; set; }
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

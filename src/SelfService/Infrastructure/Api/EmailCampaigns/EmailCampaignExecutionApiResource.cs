namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class EmailCampaignExecutionApiResource
{
    public string Id { get; set; } = "";
    public string EmailCampaignId { get; set; } = "";
    public DateTime ExecutedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string Status { get; set; } = "";
}

namespace SelfService.Domain.Models;

public class EmailCampaignStatus : ValueObjectEnum<EmailCampaignStatus>
{
    public static readonly EmailCampaignStatus Draft = new("Draft");
    public static readonly EmailCampaignStatus Scheduled = new("Scheduled");
    public static readonly EmailCampaignStatus Sending = new("Sending");
    public static readonly EmailCampaignStatus Sent = new("Sent");
    public static readonly EmailCampaignStatus Failed = new("Failed");
    public static readonly EmailCampaignStatus Cancelled = new("Cancelled");

    private EmailCampaignStatus(string value)
        : base(value) { }

    public static implicit operator EmailCampaignStatus(string text) => Parse(text);

    public static implicit operator string(EmailCampaignStatus status) => status.ToString();
}

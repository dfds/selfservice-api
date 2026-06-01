namespace SelfService.Domain.Models;

public class EmailCampaignRecipientStatus : ValueObjectEnum<EmailCampaignRecipientStatus>
{
    public static readonly EmailCampaignRecipientStatus Pending = new("Pending");
    public static readonly EmailCampaignRecipientStatus Sent = new("Sent");
    public static readonly EmailCampaignRecipientStatus Failed = new("Failed");

    private EmailCampaignRecipientStatus(string value)
        : base(value) { }

    public static implicit operator EmailCampaignRecipientStatus(string text) => Parse(text);

    public static implicit operator string(EmailCampaignRecipientStatus status) => status.ToString();
}

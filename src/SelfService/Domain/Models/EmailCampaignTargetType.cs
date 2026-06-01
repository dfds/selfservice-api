namespace SelfService.Domain.Models;

public class EmailCampaignTargetType : ValueObjectEnum<EmailCampaignTargetType>
{
    public static readonly EmailCampaignTargetType Capability = new("Capability");
    public static readonly EmailCampaignTargetType User = new("User");

    private EmailCampaignTargetType(string value)
        : base(value) { }

    public static implicit operator EmailCampaignTargetType(string text) => Parse(text);

    public static implicit operator string(EmailCampaignTargetType targetType) => targetType.ToString();
}

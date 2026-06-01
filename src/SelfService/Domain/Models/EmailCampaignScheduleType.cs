namespace SelfService.Domain.Models;

public class EmailCampaignScheduleType : ValueObjectEnum<EmailCampaignScheduleType>
{
    public static readonly EmailCampaignScheduleType Immediate = new("Immediate");
    public static readonly EmailCampaignScheduleType Scheduled = new("Scheduled");
    public static readonly EmailCampaignScheduleType Recurring = new("Recurring");

    private EmailCampaignScheduleType(string value)
        : base(value) { }

    public static implicit operator EmailCampaignScheduleType(string text) => Parse(text);

    public static implicit operator string(EmailCampaignScheduleType scheduleType) => scheduleType.ToString();
}

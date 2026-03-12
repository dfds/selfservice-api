namespace SelfService.Domain.Models;

public class EmailCampaignExecutionStatus : ValueObjectEnum<EmailCampaignExecutionStatus>
{
    public static readonly EmailCampaignExecutionStatus InProgress = new("InProgress");
    public static readonly EmailCampaignExecutionStatus Completed = new("Completed");
    public static readonly EmailCampaignExecutionStatus PartialFailure = new("PartialFailure");

    private EmailCampaignExecutionStatus(string value)
        : base(value) { }

    public static implicit operator EmailCampaignExecutionStatus(string text) => Parse(text);

    public static implicit operator string(EmailCampaignExecutionStatus status) => status.ToString();
}

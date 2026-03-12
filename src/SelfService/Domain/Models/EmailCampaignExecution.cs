namespace SelfService.Domain.Models;

public class EmailCampaignExecution : Entity<EmailCampaignExecutionId>
{
    public EmailCampaignExecution(
        EmailCampaignExecutionId id,
        EmailCampaignId emailCampaignId,
        DateTime executedAt,
        int totalRecipients,
        int successCount,
        int failureCount,
        EmailCampaignExecutionStatus status
    )
        : base(id)
    {
        EmailCampaignId = emailCampaignId;
        ExecutedAt = executedAt;
        TotalRecipients = totalRecipients;
        SuccessCount = successCount;
        FailureCount = failureCount;
        Status = status;
    }

    public EmailCampaignId EmailCampaignId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public EmailCampaignExecutionStatus Status { get; set; }

    public static EmailCampaignExecution Create(
        EmailCampaignId campaignId,
        int totalRecipients
    )
    {
        return new EmailCampaignExecution(
            id: EmailCampaignExecutionId.New(),
            emailCampaignId: campaignId,
            executedAt: DateTime.UtcNow,
            totalRecipients: totalRecipients,
            successCount: 0,
            failureCount: 0,
            status: EmailCampaignExecutionStatus.InProgress
        );
    }

    public void UpdateProgress(int successCount, int failureCount)
    {
        SuccessCount = successCount;
        FailureCount = failureCount;
    }

    public void MarkCompleted(int successCount, int failureCount)
    {
        SuccessCount = successCount;
        FailureCount = failureCount;
        Status = failureCount > 0 ? EmailCampaignExecutionStatus.PartialFailure : EmailCampaignExecutionStatus.Completed;
    }
}

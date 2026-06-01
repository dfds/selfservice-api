namespace SelfService.Domain.Models;

public class EmailCampaignRecipientLog : Entity<EmailCampaignRecipientLogId>
{
    public EmailCampaignRecipientLog(
        EmailCampaignRecipientLogId id,
        EmailCampaignId emailCampaignId,
        EmailCampaignExecutionId? executionId,
        string? capabilityId,
        string? capabilityName,
        string userId,
        string email,
        string renderedSubject,
        string renderedHtml,
        EmailCampaignRecipientStatus status,
        DateTime? sentAt,
        string? errorMessage,
        DateTime createdAt
    )
        : base(id)
    {
        EmailCampaignId = emailCampaignId;
        ExecutionId = executionId;
        CapabilityId = capabilityId;
        CapabilityName = capabilityName;
        UserId = userId;
        Email = email;
        RenderedSubject = renderedSubject;
        RenderedHtml = renderedHtml;
        Status = status;
        SentAt = sentAt;
        ErrorMessage = errorMessage;
        CreatedAt = createdAt;
    }

    public EmailCampaignId EmailCampaignId { get; set; }
    public EmailCampaignExecutionId? ExecutionId { get; set; }

    // Nullable because user-targeted campaigns are not scoped to a capability.
    public string? CapabilityId { get; set; }
    public string? CapabilityName { get; set; }
    public string UserId { get; set; }
    public string Email { get; set; }
    public string RenderedSubject { get; set; }
    public string RenderedHtml { get; set; }
    public EmailCampaignRecipientStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    public static EmailCampaignRecipientLog Create(
        EmailCampaignId campaignId,
        EmailCampaignExecutionId? executionId,
        string? capabilityId,
        string? capabilityName,
        string userId,
        string email,
        string renderedSubject,
        string renderedHtml
    )
    {
        return new EmailCampaignRecipientLog(
            id: EmailCampaignRecipientLogId.New(),
            emailCampaignId: campaignId,
            executionId: executionId,
            capabilityId: capabilityId,
            capabilityName: capabilityName,
            userId: userId,
            email: email,
            renderedSubject: renderedSubject,
            renderedHtml: renderedHtml,
            status: EmailCampaignRecipientStatus.Pending,
            sentAt: null,
            errorMessage: null,
            createdAt: DateTime.UtcNow
        );
    }

    public void MarkAsSent()
    {
        Status = EmailCampaignRecipientStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string? errorMessage)
    {
        Status = EmailCampaignRecipientStatus.Failed;
        ErrorMessage = errorMessage;
    }

    public void ResetForRetry()
    {
        Status = EmailCampaignRecipientStatus.Pending;
        ErrorMessage = null;
    }
}

using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IEmailCampaignApplicationService
{
    Task<EmailCampaign> CreateDraft(
        string name,
        string subject,
        string contentJson,
        string? contentHtml,
        string audienceJson,
        string? recipientFilter,
        string createdBy,
        EmailCampaignScheduleType? scheduleType = null,
        DateTime? scheduledAt = null,
        string? cronExpression = null
    );

    Task<EmailCampaign?> GetById(EmailCampaignId id);
    Task<List<EmailCampaign>> GetAll(string? statusFilter);
    Task UpdateDraft(
        EmailCampaignId id,
        string name,
        string subject,
        string contentJson,
        string? contentHtml,
        string audienceJson,
        string? recipientFilter,
        string modifiedBy,
        EmailCampaignScheduleType? scheduleType = null,
        DateTime? scheduledAt = null,
        string? cronExpression = null
    );
    Task DeleteDraft(EmailCampaignId id);
    Task<EmailCampaign> DuplicateCampaign(EmailCampaignId sourceId, string createdBy);
    Task<List<TemplateVariable>> GetTemplateVariables();

    Task<AudienceResolutionResult> ResolveAudience(string audienceJson, string? recipientFilter);
    Task<List<EmailPreviewResult>> PreviewCampaign(EmailCampaignId id, string[]? capabilityIds);
    Task<SendCampaignResult> SendCampaign(EmailCampaignId id, string sentBy);
    Task CancelCampaign(EmailCampaignId id);
    Task RevertToDraft(EmailCampaignId id, string modifiedBy);
    Task ScheduleCampaign(EmailCampaignId id, EmailCampaignScheduleType scheduleType, DateTime? scheduledAt, string? cronExpression, string scheduledBy);
    Task MarkCampaignAsSending(EmailCampaignId id);
    Task<SendCampaignResult> ExecuteScheduledCampaign(EmailCampaignId id);
    Task<SendCampaignResult> ExecuteRecurringCampaign(EmailCampaignId id);
    Task<List<EmailCampaignExecution>> GetExecutions(EmailCampaignId id);
    Task<List<EmailCampaignRecipientLog>> GetRecipientLog(EmailCampaignId id);
    Task<RetryResult> RetryFailedRecipients(EmailCampaignId id, string retriedBy);
    Task UpdateDeliveryStatus(EmailCampaignRecipientLogId recipientLogId, EmailCampaignRecipientStatus status, string? errorMessage);
}

public class RetryResult
{
    public int RetriedCount { get; set; }
    public string Status { get; set; } = "";
}

public class TemplateVariable
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Entity { get; set; } = "";
    public string Example { get; set; } = "";
}

public class AudienceResolutionResult
{
    public int TotalCapabilities { get; set; }
    public int TotalRecipients { get; set; }
    public List<AudienceCapabilityResult> Capabilities { get; set; } = new();
}

public class AudienceCapabilityResult
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int MemberCount { get; set; }
    public List<RecipientDto> Recipients { get; set; } = new();
}

public class RecipientDto
{
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
}

public class EmailPreviewResult
{
    public string CapabilityId { get; set; } = "";
    public string CapabilityName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Html { get; set; } = "";
}

public class SendCampaignResult
{
    public int TotalRecipients { get; set; }
    public string Status { get; set; } = "";
}

using SelfService.Domain.Models;
using SelfService.Domain.Services;

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
        string? cronExpression = null,
        EmailCampaignTargetType? targetType = null
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
        string? cronExpression = null,
        EmailCampaignTargetType? targetType = null
    );
    Task DeleteDraft(EmailCampaignId id);
    Task<EmailCampaign> DuplicateCampaign(EmailCampaignId sourceId, string createdBy);
    Task<IReadOnlyList<TemplateVariable>> GetTemplateVariables(EmailCampaignTargetType? targetType = null);

    Task<AudienceResolutionResult> ResolveAudience(string audienceJson, string? recipientFilter);
    Task<UserAudienceResolutionResult> ResolveUserAudience(string audienceJson, string? recipientFilter);
    Task<List<EmailPreviewResult>> PreviewCampaign(EmailCampaignId id, string[]? capabilityIds);
    Task<List<UserEmailPreviewResult>> PreviewUserCampaign(EmailCampaignId id, string[]? userEmails);
    Task<SendCampaignResult> SendCampaign(EmailCampaignId id, string sentBy);
    Task CancelCampaign(EmailCampaignId id);
    Task RevertToDraft(EmailCampaignId id, string modifiedBy);
    Task ScheduleCampaign(
        EmailCampaignId id,
        EmailCampaignScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression,
        string scheduledBy
    );
    Task MarkCampaignAsSending(EmailCampaignId id);
    Task<SendCampaignResult> ExecuteScheduledCampaign(EmailCampaignId id);
    Task<SendCampaignResult> ExecuteRecurringCampaign(EmailCampaignId id);
    Task<List<EmailCampaignExecution>> GetExecutions(EmailCampaignId id);
    Task<List<EmailCampaignRecipientLog>> GetRecipientLog(EmailCampaignId id);
    Task<RetryResult> RetryFailedRecipients(EmailCampaignId id, string retriedBy);
    Task UpdateDeliveryStatus(
        EmailCampaignRecipientLogId recipientLogId,
        EmailCampaignRecipientStatus status,
        string? errorMessage
    );
}

public class RetryResult
{
    public int RetriedCount { get; set; }
    public string Status { get; set; } = "";
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

public class UserAudienceResolutionResult
{
    public int TotalRecipients { get; set; }
    public List<AudienceUserResult> Users { get; set; } = new();

    /// <summary>
    /// For "specific" mode: emails the requester provided that did not match any existing user.
    /// </summary>
    public List<string> UnmatchedEmails { get; set; } = new();
}

public class AudienceUserResult
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
}

public class UserEmailPreviewResult
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Subject { get; set; } = "";
    public string Html { get; set; } = "";
}

public class SendCampaignResult
{
    public int TotalRecipients { get; set; }
    public string Status { get; set; } = "";
}

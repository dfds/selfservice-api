using System.ComponentModel.DataAnnotations.Schema;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class EmailCampaign : AggregateRoot<EmailCampaignId>
{
    public EmailCampaign(
        EmailCampaignId id,
        string name,
        string subject,
        string contentJson,
        string? contentHtml,
        string audienceJson,
        string? recipientFilter,
        EmailCampaignScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression,
        EmailCampaignStatus status,
        DateTime createdAt,
        string createdBy,
        DateTime modifiedAt,
        string modifiedBy,
        DateTime? sentAt,
        DateTime? cancelledAt,
        bool isDeleted = false
    )
        : base(id)
    {
        Name = name;
        Subject = subject;
        ContentJson = contentJson;
        ContentHtml = contentHtml;
        AudienceJson = audienceJson;
        RecipientFilter = recipientFilter;
        ScheduleType = scheduleType;
        ScheduledAt = scheduledAt;
        CronExpression = cronExpression;
        Status = status;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
        SentAt = sentAt;
        CancelledAt = cancelledAt;
        IsDeleted = isDeleted;
    }

    public string Name { get; set; }
    public string Subject { get; set; }
    public string ContentJson { get; set; }
    public string? ContentHtml { get; set; }

    [Column(TypeName = "jsonb")]
    public string AudienceJson { get; set; }

    public string? RecipientFilter { get; set; }
    public EmailCampaignScheduleType ScheduleType { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? CronExpression { get; set; }
    public EmailCampaignStatus Status { get; set; }
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public DateTime ModifiedAt { get; private set; }
    public string ModifiedBy { get; private set; }
    public DateTime? SentAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool IsDeleted { get; set; }

    public const int MaxRecipientsPerCampaign = 5000;

    public static EmailCampaign CreateDraft(
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
    )
    {
        var now = DateTime.UtcNow;
        var campaign = new EmailCampaign(
            id: EmailCampaignId.New(),
            name: name,
            subject: subject,
            contentJson: contentJson,
            contentHtml: contentHtml,
            audienceJson: audienceJson,
            recipientFilter: recipientFilter,
            scheduleType: scheduleType ?? EmailCampaignScheduleType.Immediate,
            scheduledAt: null,
            cronExpression: null,
            status: EmailCampaignStatus.Draft,
            createdAt: now,
            createdBy: createdBy,
            modifiedAt: now,
            modifiedBy: createdBy,
            sentAt: null,
            cancelledAt: null
        );

        if (scheduleType is not null)
            campaign.UpdateScheduleFields(scheduleType, scheduledAt, cronExpression);

        campaign.Raise(new EmailCampaignCreated
        {
            CampaignId = campaign.Id.ToString(),
            Name = name,
            CreatedBy = createdBy,
        });
        return campaign;
    }

    public void Update(
        string name,
        string subject,
        string contentJson,
        string? contentHtml,
        string audienceJson,
        string? recipientFilter,
        string modifiedBy
    )
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Only drafts can be updated.");

        Name = name;
        Subject = subject;
        ContentJson = contentJson;
        ContentHtml = contentHtml;
        AudienceJson = audienceJson;
        RecipientFilter = recipientFilter;
        ModifiedAt = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    public void UpdateScheduleFields(
        EmailCampaignScheduleType scheduleType,
        DateTime? scheduledAt,
        string? cronExpression)
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Only drafts can be updated.");

        if (scheduleType == EmailCampaignScheduleType.Scheduled && scheduledAt == null)
            throw new InvalidOperationException("ScheduledAt is required for one-time scheduled campaigns.");

        if (scheduleType == EmailCampaignScheduleType.Recurring && string.IsNullOrEmpty(cronExpression))
            throw new InvalidOperationException("CronExpression is required for recurring campaigns.");

        ScheduleType = scheduleType;
        ScheduledAt = scheduleType == EmailCampaignScheduleType.Recurring
            ? (scheduledAt ?? DateTime.UtcNow)
            : scheduledAt;
        CronExpression = cronExpression;
    }

    public void SetSchedule(EmailCampaignScheduleType scheduleType, DateTime? scheduledAt, string? cronExpression)
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Only drafts can be scheduled.");

        if (scheduleType == EmailCampaignScheduleType.Scheduled && scheduledAt == null)
            throw new InvalidOperationException("ScheduledAt is required for one-time scheduled campaigns.");

        if (scheduleType == EmailCampaignScheduleType.Recurring && string.IsNullOrEmpty(cronExpression))
            throw new InvalidOperationException("CronExpression is required for recurring campaigns.");

        ScheduleType = scheduleType;
        ScheduledAt = scheduleType == EmailCampaignScheduleType.Recurring ? (scheduledAt ?? DateTime.UtcNow) : scheduledAt;
        CronExpression = cronExpression;
    }

    public void MarkAsScheduled(string? scheduledBy = null)
    {
        if (Status != EmailCampaignStatus.Draft)
            throw new InvalidOperationException("Only drafts can be scheduled.");

        Status = EmailCampaignStatus.Scheduled;
        ModifiedAt = DateTime.UtcNow;
        if (scheduledBy != null)
            ModifiedBy = scheduledBy;
    }

    public void MarkAsSending()
    {
        if (Status != EmailCampaignStatus.Draft && Status != EmailCampaignStatus.Scheduled && Status != EmailCampaignStatus.Sending)
            throw new InvalidOperationException("Only drafts or scheduled campaigns can be sent.");

        Status = EmailCampaignStatus.Sending;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ResetToScheduled(string? modifiedBy = null)
    {
        if (Status != EmailCampaignStatus.Sending)
            throw new InvalidOperationException("Only campaigns in Sending status can be reset to Scheduled.");

        Status = EmailCampaignStatus.Scheduled;
        ModifiedAt = DateTime.UtcNow;
        if (modifiedBy != null)
            ModifiedBy = modifiedBy;
    }

    public void MarkAsSent(string? sentBy = null)
    {
        Status = EmailCampaignStatus.Sent;
        SentAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;
        if (sentBy != null)
            ModifiedBy = sentBy;
    }

    public void MarkAsFailed()
    {
        Status = EmailCampaignStatus.Failed;
        ModifiedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != EmailCampaignStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled campaigns can be cancelled.");

        Status = EmailCampaignStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CronExpression = null;
        ModifiedAt = DateTime.UtcNow;
    }

    public void RevertToDraft(string? modifiedBy = null)
    {
        if (Status != EmailCampaignStatus.Scheduled)
            throw new InvalidOperationException("Only scheduled campaigns can be moved back to draft.");

        Status = EmailCampaignStatus.Draft;
        ModifiedAt = DateTime.UtcNow;
        if (modifiedBy != null)
            ModifiedBy = modifiedBy;
        // ScheduleType / ScheduledAt / CronExpression intentionally preserved so the
        // editor pre-fills the previous schedule; CancelledAt/SentAt are untouched.
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        ModifiedAt = DateTime.UtcNow;
    }

    public static EmailCampaign Duplicate(EmailCampaign source, string createdBy)
    {
        var now = DateTime.UtcNow;
        var campaign = new EmailCampaign(
            id: EmailCampaignId.New(),
            name: $"{source.Name} (copy)",
            subject: source.Subject,
            contentJson: source.ContentJson,
            contentHtml: source.ContentHtml,
            audienceJson: source.AudienceJson,
            recipientFilter: source.RecipientFilter,
            scheduleType: EmailCampaignScheduleType.Immediate,
            scheduledAt: null,
            cronExpression: null,
            status: EmailCampaignStatus.Draft,
            createdAt: now,
            createdBy: createdBy,
            modifiedAt: now,
            modifiedBy: createdBy,
            sentAt: null,
            cancelledAt: null
        );

        campaign.Raise(new EmailCampaignCreated
        {
            CampaignId = campaign.Id.ToString(),
            Name = campaign.Name,
            CreatedBy = createdBy,
        });

        return campaign;
    }

    public void RaiseSendRequestedEvent(
        string recipientLogId,
        string recipientEmail,
        string renderedSubject,
        string renderedHtml
    )
    {
        Raise(new EmailCampaignSendRequested
        {
            EmailSendId = Id.ToString(),
            RecipientLogId = recipientLogId,
            RecipientEmail = recipientEmail,
            Subject = renderedSubject,
            HtmlBody = renderedHtml,
        });
    }
}

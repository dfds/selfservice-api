namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class EmailCampaignApiResource
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string ContentJson { get; set; } = "";
    public string? ContentHtml { get; set; }
    public string AudienceJson { get; set; } = "";
    public string? RecipientFilter { get; set; }
    public string TargetType { get; set; } = "Capability";
    public string ScheduleType { get; set; } = "";
    public DateTime? ScheduledAt { get; set; }
    public string? CronExpression { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = "";
    public DateTime? SentAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}

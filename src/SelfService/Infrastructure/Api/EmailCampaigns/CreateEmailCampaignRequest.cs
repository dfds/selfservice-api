using System.ComponentModel.DataAnnotations;

namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class CreateEmailCampaignRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    public string? Subject { get; set; }

    [Required]
    public string? ContentJson { get; set; }

    public string? ContentHtml { get; set; }

    [Required]
    public string? AudienceJson { get; set; }

    public string? RecipientFilter { get; set; }

    public string? ScheduleType { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? CronExpression { get; set; }

    /// <summary>
    /// "Capability" (default) or "User". Immutable after creation.
    /// </summary>
    public string? TargetType { get; set; }
}

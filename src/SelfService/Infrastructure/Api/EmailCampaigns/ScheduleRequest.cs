namespace SelfService.Infrastructure.Api.EmailCampaigns;

public class ScheduleRequest
{
    public string? ScheduleType { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? CronExpression { get; set; }
}

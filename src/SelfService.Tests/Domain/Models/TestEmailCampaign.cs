using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestEmailCampaign
{
    private static EmailCampaign ScheduledCampaign()
    {
        var campaign = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: null,
            createdBy: "tester"
        );
        campaign.SetSchedule(EmailCampaignScheduleType.Recurring, DateTime.UtcNow, "0 9 * * 1");
        campaign.MarkAsScheduled("tester");
        return campaign;
    }

    [Fact]
    public void revert_to_draft_from_scheduled_resets_status_and_preserves_schedule()
    {
        var campaign = ScheduledCampaign();
        var scheduledAt = campaign.ScheduledAt;
        var cron = campaign.CronExpression;

        campaign.RevertToDraft("editor");

        Assert.Equal(EmailCampaignStatus.Draft, campaign.Status);
        Assert.Equal(EmailCampaignScheduleType.Recurring, campaign.ScheduleType);
        Assert.Equal(scheduledAt, campaign.ScheduledAt);
        Assert.Equal(cron, campaign.CronExpression);
        Assert.Equal("editor", campaign.ModifiedBy);
    }

    [Fact]
    public void revert_to_draft_throws_when_not_scheduled()
    {
        var campaign = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: null,
            createdBy: "tester"
        );

        Assert.Throws<InvalidOperationException>(() => campaign.RevertToDraft("editor"));
    }
}

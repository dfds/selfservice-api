using System.Linq;
using SelfService.Domain.Events;
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

    [Fact]
    public void create_draft_defaults_target_type_to_capability()
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

        Assert.Equal(EmailCampaignTargetType.Capability, campaign.TargetType);
    }

    [Fact]
    public void create_draft_accepts_recipient_filter_on_user_target()
    {
        var campaign = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: "Owner,Contributor",
            createdBy: "tester",
            targetType: EmailCampaignTargetType.User
        );

        Assert.Equal(EmailCampaignTargetType.User, campaign.TargetType);
        Assert.Equal("Owner,Contributor", campaign.RecipientFilter);
    }

    [Fact]
    public void update_accepts_recipient_filter_on_user_target()
    {
        var campaign = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: null,
            createdBy: "tester",
            targetType: EmailCampaignTargetType.User
        );

        campaign.Update(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: "Owner",
            modifiedBy: "editor"
        );

        Assert.Equal("Owner", campaign.RecipientFilter);
    }

    [Fact]
    public void duplicate_carries_target_type()
    {
        var source = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: null,
            createdBy: "tester",
            targetType: EmailCampaignTargetType.User
        );

        var duplicate = EmailCampaign.Duplicate(source, "duplicator");

        Assert.Equal(EmailCampaignTargetType.User, duplicate.TargetType);
    }

    [Fact]
    public void duplicate_raises_email_campaign_created_event()
    {
        var source = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: null,
            createdBy: "tester"
        );

        var duplicate = EmailCampaign.Duplicate(source, "duplicator");

        var created = duplicate.GetEvents().OfType<EmailCampaignCreated>().Single();
        Assert.Equal(duplicate.Id.ToString(), created.CampaignId);
        Assert.Equal(duplicate.Name, created.Name);
        Assert.Equal("duplicator", created.CreatedBy);
    }

    [Fact]
    public void send_requested_with_inline_images_raises_v2_event_with_cid_body_and_descriptors()
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
        var inlineImage = new InlineEmailImage("image-123", "private-key", "image/png", "diagram.png");

        campaign.RaiseSendRequestedEvent(
            "log-id",
            "user@example.com",
            "Subject",
            "<img src=\"cid:image-123\">",
            [inlineImage]
        );

        var send = Assert.Single(campaign.GetEvents().OfType<EmailCampaignSendRequestedWithInlineImages>());
        Assert.Equal("log-id", send.RecipientLogId);
        Assert.Equal("<img src=\"cid:image-123\">", send.HtmlBody);
        Assert.Equal(inlineImage, Assert.Single(send.InlineImages));
        Assert.Empty(campaign.GetEvents().OfType<EmailCampaignSendRequested>());
    }

    [Fact]
    public void send_requested_without_inline_images_keeps_legacy_event()
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

        campaign.RaiseSendRequestedEvent("log-id", "user@example.com", "Subject", "<p>No image</p>");

        var send = Assert.Single(campaign.GetEvents().OfType<EmailCampaignSendRequested>());
        Assert.Equal("<p>No image</p>", send.HtmlBody);
        Assert.Empty(campaign.GetEvents().OfType<EmailCampaignSendRequestedWithInlineImages>());
    }
}

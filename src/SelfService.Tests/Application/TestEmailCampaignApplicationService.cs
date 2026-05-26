using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Services;

namespace SelfService.Tests.Application;

public class TestEmailCampaignApplicationService
{
    private static EmailCampaign DraftCampaign(string? recipientFilter = null) =>
        EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "<p></p>",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: recipientFilter,
            createdBy: "tester"
        );

    [Fact]
    public async Task resolve_audience_throws_when_recipient_filter_matches_no_role()
    {
        var capabilityFilter = new Mock<ICapabilityFilterService>();
        capabilityFilter
            .Setup(x => x.ResolveCapabilities(It.IsAny<string>()))
            .ReturnsAsync(new List<Capability>());

        var rbac = new Mock<IRbacApplicationService>();
        rbac.Setup(x => x.GetAssignableRoles()).ReturnsAsync(new List<RbacRole>());

        var sut = BuildService(capabilityFilter: capabilityFilter.Object, rbac: rbac.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ResolveAudience("{\"mode\":\"all\"}", "nonexistent-role"));
    }

    [Fact]
    public async Task send_campaign_marks_execution_completed_when_no_recipients()
    {
        var campaign = DraftCampaign();

        var campaignRepo = new Mock<IEmailCampaignRepository>();
        campaignRepo.Setup(x => x.FindById(It.IsAny<EmailCampaignId>())).ReturnsAsync(campaign);

        // Empty audience resolves to zero recipients (a valid, conditional outcome).
        var capabilityFilter = new Mock<ICapabilityFilterService>();
        capabilityFilter
            .Setup(x => x.ResolveCapabilities(It.IsAny<string>()))
            .ReturnsAsync(new List<Capability>());

        EmailCampaignExecution? captured = null;
        var executionRepo = new Mock<IEmailCampaignExecutionRepository>();
        executionRepo
            .Setup(x => x.Add(It.IsAny<EmailCampaignExecution>()))
            .Callback<EmailCampaignExecution>(e => captured = e)
            .Returns(Task.CompletedTask);

        var sut = BuildService(
            campaignRepo: campaignRepo.Object,
            capabilityFilter: capabilityFilter.Object,
            executionRepo: executionRepo.Object);

        var result = await sut.SendCampaign(campaign.Id, "sender");

        Assert.Equal(0, result.TotalRecipients);
        Assert.NotNull(captured);
        Assert.Equal(EmailCampaignExecutionStatus.Completed, captured!.Status);
    }

    private static EmailCampaignApplicationService BuildService(
        IEmailCampaignRepository? campaignRepo = null,
        IEmailCampaignRecipientLogRepository? recipientLogRepo = null,
        IEmailCampaignExecutionRepository? executionRepo = null,
        ICapabilityFilterService? capabilityFilter = null,
        IRbacApplicationService? rbac = null
    )
    {
        return new EmailCampaignApplicationService(
            campaignRepo ?? Mock.Of<IEmailCampaignRepository>(),
            recipientLogRepo ?? Mock.Of<IEmailCampaignRecipientLogRepository>(),
            executionRepo ?? Mock.Of<IEmailCampaignExecutionRepository>(),
            Mock.Of<ICapabilityRepository>(),
            Mock.Of<IMembershipRepository>(),
            Mock.Of<IMemberRepository>(),
            capabilityFilter ?? Mock.Of<ICapabilityFilterService>(),
            Mock.Of<ITemplateRenderingService>(),
            rbac ?? Mock.Of<IRbacApplicationService>(),
            Mock.Of<IServiceScopeFactory>()
        );
    }
}

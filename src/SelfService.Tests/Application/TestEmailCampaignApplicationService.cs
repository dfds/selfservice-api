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
        capabilityFilter.Setup(x => x.ResolveCapabilities(It.IsAny<string>())).ReturnsAsync(new List<Capability>());

        var rbac = new Mock<IRbacApplicationService>();
        rbac.Setup(x => x.GetAssignableRoles()).ReturnsAsync(new List<RbacRole>());

        var sut = BuildService(capabilityFilter: capabilityFilter.Object, rbac: rbac.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ResolveAudience("{\"mode\":\"all\"}", "nonexistent-role")
        );
    }

    [Fact]
    public async Task send_campaign_marks_execution_completed_when_no_recipients()
    {
        var campaign = DraftCampaign();

        var campaignRepo = new Mock<IEmailCampaignRepository>();
        campaignRepo.Setup(x => x.FindById(It.IsAny<EmailCampaignId>())).ReturnsAsync(campaign);

        // Empty audience resolves to zero recipients (a valid, conditional outcome).
        var capabilityFilter = new Mock<ICapabilityFilterService>();
        capabilityFilter.Setup(x => x.ResolveCapabilities(It.IsAny<string>())).ReturnsAsync(new List<Capability>());

        EmailCampaignExecution? captured = null;
        var executionRepo = new Mock<IEmailCampaignExecutionRepository>();
        executionRepo
            .Setup(x => x.Add(It.IsAny<EmailCampaignExecution>()))
            .Callback<EmailCampaignExecution>(e => captured = e)
            .Returns(Task.CompletedTask);

        var sut = BuildService(
            campaignRepo: campaignRepo.Object,
            capabilityFilter: capabilityFilter.Object,
            executionRepo: executionRepo.Object
        );

        var result = await sut.SendCampaign(campaign.Id, "sender");

        Assert.Equal(0, result.TotalRecipients);
        Assert.NotNull(captured);
        Assert.Equal(EmailCampaignExecutionStatus.Completed, captured!.Status);
    }

    [Fact]
    public async Task resolve_audience_resolves_comma_separated_role_names()
    {
        var ownerRole = new RbacRole(
            RbacRoleId.New(),
            "system",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Owner",
            "",
            RbacAccessType.Capability
        );
        var contributorRole = new RbacRole(
            RbacRoleId.New(),
            "system",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Contributor",
            "",
            RbacAccessType.Capability
        );

        var capabilityFilter = new Mock<ICapabilityFilterService>();
        capabilityFilter.Setup(x => x.ResolveCapabilities(It.IsAny<string>())).ReturnsAsync(new List<Capability>());

        var rbac = new Mock<IRbacApplicationService>();
        rbac.Setup(x => x.GetAssignableRoles()).ReturnsAsync(new List<RbacRole> { ownerRole, contributorRole });

        var sut = BuildService(capabilityFilter: capabilityFilter.Object, rbac: rbac.Object);

        // Single value, multi value, whitespace + casing tolerance — all succeed.
        var single = await sut.ResolveAudience("{\"mode\":\"all\"}", "Owner");
        var multi = await sut.ResolveAudience("{\"mode\":\"all\"}", "Owner,Contributor");
        var messy = await sut.ResolveAudience("{\"mode\":\"all\"}", " owner , CONTRIBUTOR ");

        Assert.NotNull(single);
        Assert.NotNull(multi);
        Assert.NotNull(messy);
    }

    [Fact]
    public async Task resolve_audience_throws_with_unmatched_names_listed()
    {
        var ownerRole = new RbacRole(
            RbacRoleId.New(),
            "system",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Owner",
            "",
            RbacAccessType.Capability
        );

        var capabilityFilter = new Mock<ICapabilityFilterService>();
        capabilityFilter.Setup(x => x.ResolveCapabilities(It.IsAny<string>())).ReturnsAsync(new List<Capability>());

        var rbac = new Mock<IRbacApplicationService>();
        rbac.Setup(x => x.GetAssignableRoles()).ReturnsAsync(new List<RbacRole> { ownerRole });

        var sut = BuildService(capabilityFilter: capabilityFilter.Object, rbac: rbac.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.ResolveAudience("{\"mode\":\"all\"}", "Owner,Nonexistent")
        );

        Assert.Contains("Nonexistent", ex.Message);
    }

    [Fact]
    public async Task resolve_user_audience_filters_members_by_recipient_filter_roles()
    {
        var ownerRole = new RbacRole(
            RbacRoleId.New(),
            "system",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Owner",
            "",
            RbacAccessType.Capability
        );
        var readerRole = new RbacRole(
            RbacRoleId.New(),
            "system",
            DateTime.UtcNow,
            DateTime.UtcNow,
            "Reader",
            "",
            RbacAccessType.Capability
        );

        var ownerMember = new Member(
            UserId.Parse("owner@dfds.com"),
            "owner@dfds.com",
            "Owner User",
            UserSettings.Default
        );
        var readerMember = new Member(
            UserId.Parse("reader@dfds.com"),
            "reader@dfds.com",
            "Reader User",
            UserSettings.Default
        );

        var userFilter = new Mock<IUserFilterService>();
        userFilter
            .Setup(x => x.ResolveUsers(It.IsAny<string>()))
            .ReturnsAsync(
                new UserAudienceResolution
                {
                    Members = new List<Member> { ownerMember, readerMember },
                }
            );

        var rbac = new Mock<IRbacApplicationService>();
        rbac.Setup(x => x.GetAssignableRoles()).ReturnsAsync(new List<RbacRole> { ownerRole, readerRole });
        rbac.Setup(x => x.GetRoleGrantsForUser(ownerMember.Id.ToString()))
            .ReturnsAsync(
                new List<RbacRoleGrant>
                {
                    new(
                        RbacRoleGrantId.New(),
                        ownerRole.Id,
                        DateTime.UtcNow,
                        AssignedEntityType.User,
                        ownerMember.Id.ToString(),
                        RbacAccessType.Capability,
                        "some-capability"
                    ),
                }
            );
        rbac.Setup(x => x.GetRoleGrantsForUser(readerMember.Id.ToString()))
            .ReturnsAsync(
                new List<RbacRoleGrant>
                {
                    new(
                        RbacRoleGrantId.New(),
                        readerRole.Id,
                        DateTime.UtcNow,
                        AssignedEntityType.User,
                        readerMember.Id.ToString(),
                        RbacAccessType.Capability,
                        "some-capability"
                    ),
                }
            );

        var sut = BuildService(userFilter: userFilter.Object, rbac: rbac.Object);

        var result = await sut.ResolveUserAudience("{\"mode\":\"all\"}", "Owner");

        Assert.Equal(1, result.TotalRecipients);
        Assert.Single(result.Users);
        Assert.Equal("owner@dfds.com", result.Users[0].Email);
    }

    private static EmailCampaignApplicationService BuildService(
        IEmailCampaignRepository? campaignRepo = null,
        IEmailCampaignRecipientLogRepository? recipientLogRepo = null,
        IEmailCampaignExecutionRepository? executionRepo = null,
        ICapabilityFilterService? capabilityFilter = null,
        IUserFilterService? userFilter = null,
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
            userFilter ?? Mock.Of<IUserFilterService>(),
            Mock.Of<ITemplateRenderingService>(),
            rbac ?? Mock.Of<IRbacApplicationService>(),
            Mock.Of<IServiceScopeFactory>()
        );
    }
}

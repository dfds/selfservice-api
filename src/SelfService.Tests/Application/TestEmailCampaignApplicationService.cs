using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Metrics;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;
using SelfService.Tests.Builders;

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
        var roleGrants = new List<RbacRoleGrant>
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
            new(
                RbacRoleGrantId.New(),
                readerRole.Id,
                DateTime.UtcNow,
                AssignedEntityType.User,
                readerMember.Id.ToString(),
                RbacAccessType.Capability,
                "some-capability"
            ),
        };
        rbac.Setup(x => x.GetRoleGrantsForUsers(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(roleGrants.ToLookup(g => g.AssignedEntityId));

        var sut = BuildService(userFilter: userFilter.Object, rbac: rbac.Object);

        var result = await sut.ResolveUserAudience("{\"mode\":\"all\"}", "Owner");

        Assert.Equal(1, result.TotalRecipients);
        Assert.Single(result.Users);
        Assert.Equal("owner@dfds.com", result.Users[0].Email);
    }

    [Fact]
    public async Task user_capabilities_loop_excludes_non_active_capabilities()
    {
        // Memberships linger on capabilities that are deleted / pending deletion; those must NOT
        // appear in the {{#each User.Capabilities}} loop — only active capabilities.
        var member = new Member(UserId.Parse("user@dfds.com"), "user@dfds.com", "User", UserSettings.Default);

        var activeCap = A.Capability.WithId(CapabilityId.CreateFrom("active-cap")).WithName("Active Cap").Build();
        var deletedCap = A
            .Capability.WithId(CapabilityId.CreateFrom("deleted-cap"))
            .WithName("Deleted Cap")
            .WithStatus(CapabilityStatusOptions.Deleted)
            .Build();

        var campaign = EmailCampaign.CreateDraft(
            name: "Campaign",
            subject: "Subject",
            contentJson: "{}",
            contentHtml: "{{#each User.Capabilities}}[{{Capability.Name}}]{{/each}}",
            audienceJson: "{\"mode\":\"all\"}",
            recipientFilter: null,
            createdBy: "tester"
        );

        var campaignRepo = new Mock<IEmailCampaignRepository>();
        campaignRepo.Setup(x => x.FindById(It.IsAny<EmailCampaignId>())).ReturnsAsync(campaign);

        var userFilter = new Mock<IUserFilterService>();
        userFilter
            .Setup(x => x.ResolveUsers(It.IsAny<string>()))
            .ReturnsAsync(new UserAudienceResolution { Members = new List<Member> { member } });

        var membershipRepo = new Mock<IMembershipRepository>();
        membershipRepo
            .Setup(x => x.GetAllMembershipsForUserIds(It.IsAny<IEnumerable<UserId>>()))
            .ReturnsAsync(
                new List<Membership>
                {
                    A.Membership.WithUserId(member.Id).WithCapabilityId(activeCap.Id),
                    A.Membership.WithUserId(member.Id).WithCapabilityId(deletedCap.Id),
                }
            );
        membershipRepo
            .Setup(x => x.GetMemberCountsByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new Dictionary<CapabilityId, int>());

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo
            .Setup(x => x.GetByIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new List<Capability> { activeCap, deletedCap });

        var awsRepo = new Mock<IAwsAccountRepository>();
        awsRepo
            .Setup(x => x.GetByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new List<AwsAccount>());
        var azureRepo = new Mock<IAzureResourceRepository>();
        azureRepo
            .Setup(x => x.GetForCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new List<AzureResource>());
        var appQuery = new Mock<ICapabilityMembershipApplicationQuery>();
        appQuery
            .Setup(x => x.FindPendingByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(new List<MembershipApplication>());
        var metricService = new Mock<IRequirementsMetricService>();
        metricService
            .Setup(x => x.GetRequirementScoresForCapabilitiesAsync(It.IsAny<IReadOnlyCollection<string>>()))
            .ReturnsAsync(new Dictionary<string, List<RequirementsMetric>>());

        var scopeFactory = ScopeFactoryWith(
            (typeof(IMembershipRepository), membershipRepo.Object),
            (typeof(ICapabilityRepository), capabilityRepo.Object),
            (typeof(IAwsAccountRepository), awsRepo.Object),
            (typeof(IAzureResourceRepository), azureRepo.Object),
            (typeof(ICapabilityMembershipApplicationQuery), appQuery.Object),
            (typeof(IRequirementsMetricService), metricService.Object)
        );

        var sut = BuildService(
            campaignRepo: campaignRepo.Object,
            userFilter: userFilter.Object,
            templateRendering: new TemplateRenderingService(),
            scopeFactory: scopeFactory
        );

        var previews = await sut.PreviewUserCampaign(campaign.Id, new[] { member.Email });

        Assert.Single(previews);
        Assert.Contains("Active Cap", previews[0].Html);
        Assert.DoesNotContain("Deleted Cap", previews[0].Html);
    }

    private static EmailCampaignApplicationService BuildService(
        IEmailCampaignRepository? campaignRepo = null,
        IEmailCampaignRecipientLogRepository? recipientLogRepo = null,
        IEmailCampaignExecutionRepository? executionRepo = null,
        ICapabilityFilterService? capabilityFilter = null,
        IUserFilterService? userFilter = null,
        IRbacApplicationService? rbac = null,
        ITemplateRenderingService? templateRendering = null,
        IServiceScopeFactory? scopeFactory = null,
        AllCapabilitiesCostsCache? costsCache = null
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
            templateRendering ?? Mock.Of<ITemplateRenderingService>(),
            rbac ?? Mock.Of<IRbacApplicationService>(),
            scopeFactory ?? Mock.Of<IServiceScopeFactory>(),
            costsCache ?? new AllCapabilitiesCostsCache()
        );
    }

    // Wires an IServiceScopeFactory whose scope resolves the given service instances — mirrors the
    // scoped resolution LoadUserCapabilitiesForMembers performs at runtime.
    private static IServiceScopeFactory ScopeFactoryWith(params (Type type, object impl)[] services)
    {
        var provider = new Mock<IServiceProvider>();
        foreach (var (type, impl) in services)
            provider.Setup(p => p.GetService(type)).Returns(impl);

        var scope = new Mock<IServiceScope>();
        scope.SetupGet(s => s.ServiceProvider).Returns(provider.Object);

        var factory = new Mock<IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        return factory.Object;
    }
}

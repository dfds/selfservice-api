using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SelfService.Application;
using SelfService.Domain.Events;
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

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["SS_PORTAL_BASE_URL"]).Returns("localhost:3001");

        var sut = BuildService(
            campaignRepo: campaignRepo.Object,
            userFilter: userFilter.Object,
            templateRendering: new TemplateRenderingService(configMock.Object),
            scopeFactory: scopeFactory
        );

        var previews = await sut.PreviewUserCampaign(campaign.Id, new[] { member.Email });

        Assert.Single(previews);
        Assert.Contains("Active Cap", previews[0].Html);
        Assert.DoesNotContain("Deleted Cap", previews[0].Html);
    }

    [Theory]
    [InlineData("manual", false)]
    [InlineData("scheduled", false)]
    [InlineData("recurring", false)]
    [InlineData("manual", true)]
    [InlineData("scheduled", true)]
    [InlineData("recurring", true)]
    public async Task every_send_entry_point_resolves_owner_images_once(string mode, bool userTarget)
    {
        var campaign = EmailCampaign.CreateDraft(
            "Campaign",
            "Subject",
            "{}",
            "<img src='cid:banner'>",
            "{}",
            null,
            "tester",
            targetType: userTarget ? EmailCampaignTargetType.User : EmailCampaignTargetType.Capability
        );
        if (mode != "manual")
            campaign.MarkAsScheduled();
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(campaign.Id)).ReturnsAsync(campaign);
        var assets = new Mock<IPrivateAssetRepository>();
        assets
            .Setup(repository => repository.GetByOwner(PrivateAssetOwnerType.EmailCampaign, (Guid)campaign.Id))
            .ReturnsAsync([Image(campaign)]);
        var capabilities = new Mock<ICapabilityFilterService>();
        capabilities
            .Setup(service => service.ResolveCapabilities(It.IsAny<string>()))
            .ReturnsAsync(new List<Capability>());
        var users = new Mock<IUserFilterService>();
        users
            .Setup(service => service.ResolveUsers(It.IsAny<string>()))
            .ReturnsAsync(new UserAudienceResolution { Members = new List<Member>() });
        var service = BuildService(
            campaignRepo: campaigns.Object,
            assetRepo: assets.Object,
            capabilityFilter: capabilities.Object,
            userFilter: users.Object
        );

        if (mode == "manual")
            await service.SendCampaign(campaign.Id, "tester");
        else if (mode == "scheduled")
            await service.ExecuteScheduledCampaign(campaign.Id);
        else
            await service.ExecuteRecurringCampaign(campaign.Id);

        assets.Verify(
            repository => repository.GetByOwner(PrivateAssetOwnerType.EmailCampaign, (Guid)campaign.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task retry_uses_canonical_metadata_for_all_recipient_events()
    {
        var campaign = DraftCampaign();
        campaign.MarkAsSent();
        campaign.ClearEvents();
        var image = Image(campaign);
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(campaign.Id)).ReturnsAsync(campaign);
        var assets = new Mock<IPrivateAssetRepository>();
        assets
            .Setup(repository => repository.GetByOwner(PrivateAssetOwnerType.EmailCampaign, (Guid)campaign.Id))
            .ReturnsAsync([image]);
        var logs = new Mock<IEmailCampaignRecipientLogRepository>();
        var failed = new[] { "first@dfds.com", "second@dfds.com" }
            .Select(email =>
            {
                var log = EmailCampaignRecipientLog.Create(
                    campaign.Id,
                    null,
                    null,
                    null,
                    email,
                    email,
                    "Subject",
                    "<img src='cid:banner'>"
                );
                log.MarkAsFailed("Failed");
                return log;
            })
            .ToList();
        logs.Setup(repository => repository.GetFailedByCampaignId(campaign.Id)).ReturnsAsync(failed);

        await BuildService(campaignRepo: campaigns.Object, assetRepo: assets.Object, recipientLogRepo: logs.Object)
            .RetryFailedRecipients(campaign.Id, "tester");

        var events = campaign.GetEvents().OfType<EmailCampaignSendRequested>().ToList();
        Assert.Equal(2, events.Count);
        foreach (var send in events)
        {
            var attachment = Assert.Single(send.Attachments);
            Assert.Equal(image.StorageKey, attachment.StorageKey);
            Assert.Equal(image.Id.ToString(), attachment.AssetId);
            Assert.True(attachment.IsInline);
        }
        assets.Verify(
            repository => repository.GetByOwner(PrivateAssetOwnerType.EmailCampaign, (Guid)campaign.Id),
            Times.Once
        );
    }

    [Fact]
    public async Task mismatched_inline_content_id_is_not_rejected_by_campaign_validation()
    {
        var campaign = DraftCampaign();
        campaign.Update(
            campaign.Name,
            campaign.Subject,
            "{}",
            "<img src='CID:missing'>",
            campaign.AudienceJson,
            null,
            "tester"
        );
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(campaign.Id)).ReturnsAsync(campaign);

        await BuildService(campaignRepo: campaigns.Object)
            .UpdateDraft(
                campaign.Id,
                campaign.Name,
                campaign.Subject,
                campaign.ContentJson,
                campaign.ContentHtml,
                campaign.AudienceJson,
                null,
                "tester"
            );

        Assert.Equal("<img src='CID:missing'>", campaign.ContentHtml);
    }

    [Fact]
    public async Task update_treats_content_json_as_frontend_owned_data()
    {
        var campaign = DraftCampaign();
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(campaign.Id)).ReturnsAsync(campaign);
        var foreignId = PrivateAssetId.New().ToString();
        var json = "{\"type\":\"image\",\"attrs\":{\"assetId\":\"" + foreignId + "\"}}";

        await BuildService(campaignRepo: campaigns.Object)
            .UpdateDraft(campaign.Id, "Changed", campaign.Subject, json, null, campaign.AudienceJson, null, "tester");

        Assert.Equal("Changed", campaign.Name);
        Assert.Equal(json, campaign.ContentJson);
    }

    [Fact]
    public async Task duplicate_does_not_copy_or_reassign_images()
    {
        var source = DraftCampaign();
        var image = Image(source);
        var json =
            "{\"type\":\"image\",\"attrs\":{\"assetId\":\""
            + image.Id
            + "\",\"src\":\"cid:banner\"},\"text\":\""
            + image.Id
            + "\"}";
        source.Update(source.Name, source.Subject, json, "<img src='cid:banner'>", source.AudienceJson, null, "tester");
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(source.Id)).ReturnsAsync(source);
        var assets = new Mock<IPrivateAssetRepository>(MockBehavior.Strict);

        var service = BuildService(campaignRepo: campaigns.Object, assetRepo: assets.Object);
        var duplicate = await service.DuplicateCampaign(source.Id, "copier");

        assets.VerifyNoOtherCalls();
        Assert.NotEqual(source.Id, duplicate.Id);
        Assert.Equal((Guid)source.Id, image.OwnerId);
        Assert.Equal(json, duplicate.ContentJson);
        Assert.Equal(json, source.ContentJson);
        Assert.Equal(source.ContentHtml, duplicate.ContentHtml);
    }

    [Fact]
    public async Task update_draft_prunes_stored_assets_not_in_the_provided_list()
    {
        var campaign = DraftCampaign();
        var keep = Image(campaign);
        var remove = Image(campaign);
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(campaign.Id)).ReturnsAsync(campaign);
        var assets = new Mock<IPrivateAssetRepository>();
        assets
            .Setup(repository => repository.GetByOwner(PrivateAssetOwnerType.EmailCampaign, (Guid)campaign.Id))
            .ReturnsAsync([keep, remove]);

        await BuildService(campaignRepo: campaigns.Object, assetRepo: assets.Object)
            .UpdateDraft(
                campaign.Id,
                "Campaign",
                "Subject",
                "{}",
                "<p></p>",
                "{\"mode\":\"all\"}",
                null,
                "tester",
                assetIds: new[] { keep.Id.ToString() }
            );

        assets.Verify(repository => repository.Remove(remove), Times.Once);
        assets.Verify(repository => repository.Remove(keep), Times.Never);
    }

    [Fact]
    public async Task update_draft_leaves_assets_untouched_when_no_asset_list_is_provided()
    {
        var campaign = DraftCampaign();
        var campaigns = new Mock<IEmailCampaignRepository>();
        campaigns.Setup(repository => repository.FindById(campaign.Id)).ReturnsAsync(campaign);
        var assets = new Mock<IPrivateAssetRepository>(MockBehavior.Strict);

        await BuildService(campaignRepo: campaigns.Object, assetRepo: assets.Object)
            .UpdateDraft(campaign.Id, "Campaign", "Subject", "{}", "<p></p>", "{\"mode\":\"all\"}", null, "tester");

        assets.VerifyNoOtherCalls();
    }

    private static PrivateAsset Image(EmailCampaign owner) =>
        new(
            PrivateAssetId.New(),
            "private-assets/source/banner.png",
            "banner.png",
            "image/png",
            4,
            "tester",
            DateTime.UtcNow,
            owner.Id,
            PrivateAssetOwnerType.EmailCampaign,
            true
        );

    private static EmailCampaignApplicationService BuildService(
        IEmailCampaignRepository? campaignRepo = null,
        IEmailCampaignRecipientLogRepository? recipientLogRepo = null,
        IEmailCampaignExecutionRepository? executionRepo = null,
        ICapabilityFilterService? capabilityFilter = null,
        IUserFilterService? userFilter = null,
        IRbacApplicationService? rbac = null,
        ITemplateRenderingService? templateRendering = null,
        IServiceScopeFactory? scopeFactory = null,
        AllCapabilitiesCostsCache? costsCache = null,
        IPrivateAssetRepository? assetRepo = null
    )
    {
        var defaultAssets = new Mock<IPrivateAssetRepository>();
        defaultAssets
            .Setup(repository => repository.GetByOwner(It.IsAny<PrivateAssetOwnerType>(), It.IsAny<Guid>()))
            .ReturnsAsync(new List<PrivateAsset>());
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
            costsCache ?? new AllCapabilitiesCostsCache(),
            assetRepo ?? defaultAssets.Object
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

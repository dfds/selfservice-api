using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;
using SelfService.Tests.Builders;

namespace SelfService.Tests.Infrastructure.Persistence;

public class TestTemplateRenderingService
{
    private readonly TemplateRenderingService _sut = new();

    private static TemplateRenderContext CreateContext(
        Capability? capability = null,
        Member? member = null,
        AwsAccount? awsAccount = null,
        List<AzureResource>? azureResources = null,
        List<RequirementsMetric>? requirementScores = null,
        int pendingMembershipApplicationCount = 0,
        CapabilityCosts? costs = null
    )
    {
        return new TemplateRenderContext
        {
            Capability = capability ?? A.Capability.Build(),
            Member = member,
            CampaignName = "Test Campaign",
            MemberCount = 5,
            AwsAccount = awsAccount,
            AzureResources = azureResources ?? new List<AzureResource>(),
            RequirementScores = requirementScores ?? new List<RequirementsMetric>(),
            PendingMembershipApplicationCount = pendingMembershipApplicationCount,
            Costs = costs,
        };
    }

    [Fact]
    public void RenderTemplate_ExistingCapabilityVariables_StillWork()
    {
        var capability = A.Capability.WithName("My Capability").WithDescription("A test capability").Build();
        var context = CreateContext(capability: capability);

        var result = _sut.RenderTemplate(
            "{{Capability.Name}} - {{Capability.Description}} ({{Capability.MemberCount}} members)",
            context
        );

        Assert.Equal("My Capability - A test capability (5 members)", result);
    }

    [Fact]
    public void RenderTemplate_MemberVariables_StillWork()
    {
        var member = A.Member.WithDisplayName("Jane Doe").Build();
        var context = CreateContext(member: member);

        var result = _sut.RenderTemplate("Hello {{Member.DisplayName}}", context);

        Assert.Equal("Hello Jane Doe", result);
    }

    [Fact]
    public void RenderTemplate_MemberVariables_NullMember_ShowsPlaceholder()
    {
        var context = CreateContext(member: null);

        var result = _sut.RenderTemplate("Hello {{Member.DisplayName}}", context);

        Assert.Equal("Hello [Member Name]", result);
    }

    [Fact]
    public void RenderTemplate_CampaignName_Renders()
    {
        var context = CreateContext();

        var result = _sut.RenderTemplate("Campaign: {{Campaign.Name}}", context);

        Assert.Equal("Campaign: Test Campaign", result);
    }

    // 5 of the 6 mandatory tags present → 83.33 → "83". Non-tags scores still come from the requirements DB.
    private const string FivePresentTagsMetadata =
        "{\"dfds.cost.centre\":\"cc\",\"dfds.businessCapability\":\"bc\",\"dfds.env\":\"prod\","
        + "\"dfds.data.classification\":\"internal\",\"dfds.service.criticality\":\"high\"}";

    private const string AllTagsMetadata =
        "{\"dfds.cost.centre\":\"cc\",\"dfds.businessCapability\":\"bc\",\"dfds.env\":\"prod\","
        + "\"dfds.data.classification\":\"internal\",\"dfds.service.criticality\":\"high\","
        + "\"dfds.service.availability\":\"99.9\"}";

    [Fact]
    public void RenderTemplate_RequirementScore_RendersValue()
    {
        // Tags derive from the capability metadata (like the compliance endpoint); other scores from the DB.
        var capability = A.Capability.WithJsonMetadata(FivePresentTagsMetadata).Build();
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "external_secrets",
                Value = 100,
                DisplayName = "External Secrets Adoption",
            },
        };
        var context = CreateContext(capability: capability, requirementScores: scores);

        var result = _sut.RenderTemplate(
            "Tags: {{Requirement.mandatory_tags}}, Secrets: {{Requirement.external_secrets}}",
            context
        );

        Assert.Equal("Tags: 83, Secrets: 100", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_Tags_IgnoresRequirementsDbMetric()
    {
        // A stale mandatory_tags metric in the requirements DB must not override the metadata-derived score.
        var capability = A.Capability.WithJsonMetadata(AllTagsMetadata).Build();
        var scores = new List<RequirementsMetric>
        {
            new() { RequirementId = "mandatory_tags", Value = 12 },
        };
        var context = CreateContext(capability: capability, requirementScores: scores);

        var result = _sut.RenderTemplate("{{Requirement.mandatory_tags}}", context);

        Assert.Equal("100", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_DisplayName_Renders()
    {
        // Non-tags requirements take DisplayName from the requirements DB metric.
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "external_secrets",
                Value = 80,
                DisplayName = "External Secrets Adoption",
            },
        };
        var context = CreateContext(requirementScores: scores);

        var result = _sut.RenderTemplate("{{Requirement.external_secrets.DisplayName}}", context);

        Assert.Equal("External Secrets Adoption", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_Tags_DisplayNameAndHelpUrl_UseComplianceConstants()
    {
        // Tags DisplayName/HelpUrl mirror the compliance endpoint, independent of the requirements DB.
        var context = CreateContext();

        Assert.Equal(
            TagComplianceEvaluator.DisplayName,
            _sut.RenderTemplate("{{Requirement.mandatory_tags.DisplayName}}", context)
        );
        Assert.Equal(
            TagComplianceEvaluator.HelpUrl,
            _sut.RenderTemplate("{{Requirement.mandatory_tags.HelpUrl}}", context)
        );
    }

    [Fact]
    public void RenderTemplate_RequirementScore_HelpUrl_Renders()
    {
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "irsa",
                Value = 50,
                HelpUrl = "https://wiki.example.com/irsa",
            },
        };
        var context = CreateContext(requirementScores: scores);

        var result = _sut.RenderTemplate("{{Requirement.irsa.HelpUrl}}", context);

        Assert.Equal("https://wiki.example.com/irsa", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_MissingDisplayName_FallsBackToId()
    {
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "irsa",
                Value = 50,
                DisplayName = null,
            },
        };
        var context = CreateContext(requirementScores: scores);

        var result = _sut.RenderTemplate("{{Requirement.irsa.DisplayName}}", context);

        Assert.Equal("irsa", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_UnknownId_RendersNA()
    {
        var scores = new List<RequirementsMetric>
        {
            new() { RequirementId = "mandatory_tags", Value = 80 },
        };
        var context = CreateContext(requirementScores: scores);

        var result = _sut.RenderTemplate("{{Requirement.unknown_id}}", context);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_EmptyScores_RendersNA()
    {
        // Non-tags requirements with no DB metric still fall back to N/A.
        var context = CreateContext(requirementScores: new List<RequirementsMetric>());

        var result = _sut.RenderTemplate("{{Requirement.external_secrets}}", context);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_Tags_NoTagsInMetadata_RendersZero()
    {
        // Default capability metadata "{}" has none of the required tags → 0 (not N/A).
        var context = CreateContext(requirementScores: new List<RequirementsMetric>());

        var result = _sut.RenderTemplate("{{Requirement.mandatory_tags}}", context);

        Assert.Equal("0", result);
    }

    [Fact]
    public void RenderTemplate_AwsAccount_Present_RendersFields()
    {
        var awsAccount = A.AwsAccount.Build();
        awsAccount.RegisterRealAwsAccount("123456789012", "aws.role@dfds.com", DateTime.UtcNow);
        awsAccount.LinkKubernetesNamespace("my-capability-ns", DateTime.UtcNow);
        var context = CreateContext(awsAccount: awsAccount);

        var result = _sut.RenderTemplate(
            "Account: {{Aws.AccountId}}, Status: {{Aws.Status}}, NS: {{Aws.Namespace}}, Email: {{Aws.RoleEmail}}",
            context
        );

        Assert.Equal(
            "Account: 123456789012, Status: Completed, NS: my-capability-ns, Email: aws.role@dfds.com",
            result
        );
    }

    [Fact]
    public void RenderTemplate_AwsAccount_Null_RendersNA()
    {
        var context = CreateContext(awsAccount: null);

        var result = _sut.RenderTemplate(
            "Account: {{Aws.AccountId}}, Status: {{Aws.Status}}, NS: {{Aws.Namespace}}, Email: {{Aws.RoleEmail}}",
            context
        );

        Assert.Equal("Account: N/A, Status: N/A, NS: N/A, Email: N/A", result);
    }

    [Fact]
    public void RenderTemplate_AwsAccount_Requested_RendersRequestedStatus()
    {
        var awsAccount = A.AwsAccount.Build();
        var context = CreateContext(awsAccount: awsAccount);

        var result = _sut.RenderTemplate("{{Aws.Status}}", context);

        Assert.Equal("Requested", result);
    }

    [Fact]
    public void RenderTemplate_AzureResources_Present_RendersCountAndEnvironments()
    {
        var resources = new List<AzureResource>
        {
            A.AzureResource.WithEnvironment("prod").Build(),
            A.AzureResource.WithEnvironment("dev").Build(),
        };
        var context = CreateContext(azureResources: resources);

        var result = _sut.RenderTemplate("Count: {{Azure.ResourceCount}}, Envs: {{Azure.Environments}}", context);

        Assert.Equal("Count: 2, Envs: dev, prod", result);
    }

    [Fact]
    public void RenderTemplate_AzureResources_Empty_RendersZeroAndNone()
    {
        var context = CreateContext(azureResources: new List<AzureResource>());

        var result = _sut.RenderTemplate("Count: {{Azure.ResourceCount}}, Envs: {{Azure.Environments}}", context);

        Assert.Equal("Count: 0, Envs: None", result);
    }

    [Fact]
    public void RenderTemplate_AzureResources_PerEnvironmentId_Renders()
    {
        var devResource = A.AzureResource.WithEnvironment("dev").Build();
        var resources = new List<AzureResource> { devResource };
        var context = CreateContext(azureResources: resources);

        var result = _sut.RenderTemplate("Dev ID: {{Azure.dev.Id}}", context);

        Assert.Equal($"Dev ID: {devResource.Id}", result);
    }

    [Fact]
    public void RenderTemplate_AzureResources_UnknownEnvironment_LeavesTokenUnreplaced()
    {
        var resources = new List<AzureResource> { A.AzureResource.WithEnvironment("dev").Build() };
        var context = CreateContext(azureResources: resources);

        var result = _sut.RenderTemplate("{{Azure.staging.Id}}", context);

        Assert.Equal("{{Azure.staging.Id}}", result);
    }

    [Fact]
    public void RenderTemplate_PendingMembershipApplicationCount_Renders()
    {
        var context = CreateContext(pendingMembershipApplicationCount: 7);

        var result = _sut.RenderTemplate("Pending: {{MembershipApplications.PendingCount}}", context);

        Assert.Equal("Pending: 7", result);
    }

    [Fact]
    public void RenderTemplate_PendingMembershipApplicationCount_Zero_RendersZero()
    {
        var context = CreateContext(pendingMembershipApplicationCount: 0);

        var result = _sut.RenderTemplate("Pending: {{MembershipApplications.PendingCount}}", context);

        Assert.Equal("Pending: 0", result);
    }

    [Fact]
    public void RenderTemplate_CapabilityCost_WithData_RendersUsdSumOverWindow()
    {
        var cap = A.Capability.Build();
        var costs = new CapabilityCosts(
            cap.Id,
            new[]
            {
                new TimeSeries(10.00f, new DateTime(2026, 6, 20)),
                new TimeSeries(20.50f, new DateTime(2026, 6, 21)),
                new TimeSeries(12.95f, new DateTime(2026, 6, 22)),
            }
        );
        var context = CreateContext(capability: cap, costs: costs);

        var result = _sut.RenderTemplate("{{Capability.Cost.7Days}}", context);

        Assert.Equal("$43.45", result);
    }

    [Fact]
    public void RenderTemplate_CapabilityCost_WindowsTakeMostRecentDays()
    {
        var cap = A.Capability.Build();
        // 30 days of $2/day, plus one much older $1000 day that the 7/14/30-day windows exclude.
        var points = new List<TimeSeries> { new(1000.00f, new DateTime(2026, 1, 1)) };
        for (var d = 0; d < 30; d++)
            points.Add(new TimeSeries(2.00f, new DateTime(2026, 6, 28).AddDays(-d)));
        var context = CreateContext(capability: cap, costs: new CapabilityCosts(cap.Id, points.ToArray()));

        var result = _sut.RenderTemplate(
            "{{Capability.Cost.7Days}}|{{Capability.Cost.14Days}}|{{Capability.Cost.30Days}}",
            context
        );

        Assert.Equal("$14.00|$28.00|$60.00", result);
    }

    [Fact]
    public void RenderTemplate_CapabilityCost_NoData_RendersNA()
    {
        var context = CreateContext(costs: null);

        var result = _sut.RenderTemplate("{{Capability.Cost.30Days}}", context);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public void RenderTemplate_EachUserCapabilities_CostResolvesPerCapability()
    {
        var capA = A.Capability.WithName("Cap A").Build();
        var capB = A.Capability.WithName("Cap B").Build();
        var ctx = UserContext(
            userCapabilities: new List<UserCapabilityRef>
            {
                new()
                {
                    Capability = capA,
                    Costs = new CapabilityCosts(capA.Id, new[] { new TimeSeries(100.00f, new DateTime(2026, 6, 28)) }),
                },
                new()
                {
                    Capability = capB,
                    Costs = new CapabilityCosts(capB.Id, new[] { new TimeSeries(5.50f, new DateTime(2026, 6, 28)) }),
                },
            }
        );

        var result = _sut.RenderTemplate(
            "{{#each User.Capabilities}}[{{Capability.Name}}={{Capability.Cost.30Days}}]{{/each}}",
            ctx
        );

        Assert.Equal("[Cap A=$100.00][Cap B=$5.50]", result);
    }

    [Fact]
    public void RenderTemplate_AllNewVariablesCombined()
    {
        var capability = A.Capability.WithJsonMetadata(AllTagsMetadata).Build();
        var awsAccount = A.AwsAccount.Build();
        awsAccount.RegisterRealAwsAccount("999888777666", "role@dfds.com", DateTime.UtcNow);
        var resources = new List<AzureResource> { A.AzureResource.WithEnvironment("dev").Build() };
        var context = CreateContext(
            capability: capability,
            awsAccount: awsAccount,
            azureResources: resources,
            pendingMembershipApplicationCount: 2
        );

        var result = _sut.RenderTemplate(
            "Tags={{Requirement.mandatory_tags}}, AWS={{Aws.AccountId}}, Azure={{Azure.ResourceCount}}, Apps={{MembershipApplications.PendingCount}}",
            context
        );

        Assert.Equal("Tags=100, AWS=999888777666, Azure=1, Apps=2", result);
    }

    [Fact]
    public void GetVariableDefinitions_ReturnsExpectedSet()
    {
        var names = _sut.GetVariableDefinitions().Select(v => v.Name).ToHashSet();

        var expected = new HashSet<string>
        {
            "Capability.Id",
            "Capability.Name",
            "Capability.Description",
            "Capability.Status",
            "Capability.CreatedAt",
            "Capability.CreatedBy",
            "Capability.RequirementScore",
            "Capability.MemberCount",
            "Capability.Cost.7Days",
            "Capability.Cost.14Days",
            "Capability.Cost.30Days",
            "Member.DisplayName",
            "Member.Email",
            "Campaign.Name",
            "Date.Today",
            "Date.Year",
            "Requirement.<id>",
            "Requirement.<id>.DisplayName",
            "Requirement.<id>.HelpUrl",
            "Aws.AccountId",
            "Aws.Status",
            "Aws.Namespace",
            "Aws.RoleEmail",
            "Azure.ResourceCount",
            "Azure.Environments",
            "Azure.<env>.Id",
            "MembershipApplications.PendingCount",
        };

        Assert.Equal(expected, names);
    }

    [Fact]
    public void RenderTemplate_WithExpression_MemberChangesButCapabilityDataPreserved()
    {
        var scores = new List<RequirementsMetric>
        {
            new() { RequirementId = "irsa", Value = 75 },
        };
        var baseContext = CreateContext(requirementScores: scores, pendingMembershipApplicationCount: 3);

        var member1 = A.Member.WithDisplayName("Alice").Build();
        var member2 = A.Member.WithDisplayName("Bob").Build();

        var result1 = _sut.RenderTemplate(
            "{{Member.DisplayName}}: {{Requirement.irsa}} ({{MembershipApplications.PendingCount}})",
            baseContext with
            {
                Member = member1,
            }
        );
        var result2 = _sut.RenderTemplate(
            "{{Member.DisplayName}}: {{Requirement.irsa}} ({{MembershipApplications.PendingCount}})",
            baseContext with
            {
                Member = member2,
            }
        );

        Assert.Equal("Alice: 75 (3)", result1);
        Assert.Equal("Bob: 75 (3)", result2);
    }

    private static TemplateRenderContext UserContext(
        Member? member = null,
        List<UserCapabilityRef>? userCapabilities = null
    )
    {
        return new TemplateRenderContext
        {
            Capability = null,
            Member = member ?? A.Member.Build(),
            CampaignName = "User Campaign",
            UserCapabilities = userCapabilities ?? new List<UserCapabilityRef>(),
        };
    }

    [Fact]
    public void RenderTemplate_UserScalars_Render()
    {
        var member = A.Member.WithDisplayName("Jane Doe").Build();
        var capA = A.Capability.WithName("Cap A").Build();
        var capB = A.Capability.WithName("Cap B").Build();
        var ctx = UserContext(
            member: member,
            userCapabilities: new List<UserCapabilityRef>
            {
                new() { Capability = capA, MemberCount = 4 },
                new() { Capability = capB, MemberCount = 7 },
            }
        );

        var result = _sut.RenderTemplate(
            "Hello {{User.DisplayName}}, you belong to {{User.CapabilityCount}} caps: {{User.CapabilityNames}}",
            ctx
        );

        Assert.Equal("Hello Jane Doe, you belong to 2 caps: Cap A, Cap B", result);
    }

    [Fact]
    public void RenderTemplate_EachUserCapabilities_RendersOnePerCapability()
    {
        var capA = A.Capability.WithName("Cap A").Build();
        var capB = A.Capability.WithName("Cap B").Build();
        var ctx = UserContext(
            userCapabilities: new List<UserCapabilityRef>
            {
                new() { Capability = capA, MemberCount = 4 },
                new() { Capability = capB, MemberCount = 7 },
            }
        );

        var result = _sut.RenderTemplate(
            "<ul>{{#each User.Capabilities}}<li>{{Capability.Name}} ({{Capability.MemberCount}})</li>{{/each}}</ul>",
            ctx
        );

        Assert.Equal("<ul><li>Cap A (4)</li><li>Cap B (7)</li></ul>", result);
    }

    [Fact]
    public void RenderTemplate_EachUserCapabilities_RendersPerCapabilityRequirementAndCounts()
    {
        // Regression: per-capability data (requirement scores, pending applications, Azure) must be
        // carried into each {{#each User.Capabilities}} iteration instead of always rendering "N/A".
        var capA = A.Capability.WithName("Cap A").Build();
        var capB = A.Capability.WithName("Cap B").Build();
        var ctx = UserContext(
            userCapabilities: new List<UserCapabilityRef>
            {
                new()
                {
                    Capability = capA,
                    MemberCount = 4,
                    RequirementScores = new List<RequirementsMetric>
                    {
                        new() { RequirementId = "external_secrets", Value = 100 },
                    },
                    PendingMembershipApplicationCount = 2,
                },
                new()
                {
                    Capability = capB,
                    MemberCount = 7,
                    RequirementScores = new List<RequirementsMetric>
                    {
                        new() { RequirementId = "external_secrets", Value = 40 },
                    },
                    PendingMembershipApplicationCount = 0,
                },
            }
        );

        var result = _sut.RenderTemplate(
            "{{#each User.Capabilities}}[{{Capability.Name}}: secrets={{Requirement.external_secrets}}, pending={{MembershipApplications.PendingCount}}]{{/each}}",
            ctx
        );

        Assert.Equal("[Cap A: secrets=100, pending=2][Cap B: secrets=40, pending=0]", result);
    }

    [Fact]
    public void RenderTemplate_EachUserCapabilities_MissingRequirement_FallsBackToNA()
    {
        // A capability with no requirement scores still falls back to N/A (no data leaks in).
        var cap = A.Capability.WithName("Cap A").Build();
        var ctx = UserContext(
            userCapabilities: new List<UserCapabilityRef>
            {
                new() { Capability = cap, MemberCount = 1 },
            }
        );

        var result = _sut.RenderTemplate("{{#each User.Capabilities}}{{Requirement.external_secrets}}{{/each}}", ctx);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public void RenderTemplate_EachUserCapabilities_EmptyList_RendersNothingForBlock()
    {
        var ctx = UserContext(userCapabilities: new List<UserCapabilityRef>());

        // The surrounding text outside the block (and other tokens) must still render.
        var result = _sut.RenderTemplate(
            "Before {{#each User.Capabilities}}<li>{{Capability.Name}}</li>{{/each}} {{Campaign.Name}}",
            ctx
        );

        Assert.Equal("Before  User Campaign", result);
    }

    [Fact]
    public void RenderTemplate_EachUserCapabilities_OuterTokensStillResolve()
    {
        var member = A.Member.WithDisplayName("Jane").Build();
        var capA = A.Capability.WithName("Cap A").Build();
        var ctx = UserContext(
            member: member,
            userCapabilities: new List<UserCapabilityRef>
            {
                new() { Capability = capA, MemberCount = 1 },
            }
        );

        var result = _sut.RenderTemplate(
            "{{User.DisplayName}}: {{#each User.Capabilities}}{{Capability.Name}}{{/each}} - {{Campaign.Name}}",
            ctx
        );

        Assert.Equal("Jane: Cap A - User Campaign", result);
    }

    [Fact]
    public void GetVariableDefinitions_User_IncludesPerCapabilityVariablesWithScope()
    {
        var userVars = _sut.GetVariableDefinitions(EmailCampaignTargetType.User);

        // Top-level user/shared variables resolve at the recipient/campaign root.
        Assert.Contains(userVars, v => v.Name == "User.Email" && v.Scope == "topLevel");
        Assert.Contains(userVars, v => v.Name == "User.CapabilityCount" && v.Scope == "topLevel");
        Assert.Contains(userVars, v => v.Name == "Campaign.Name" && v.Scope == "topLevel");

        // Capability-scoped variables now surface for use inside {{#each User.Capabilities}},
        // flagged "perCapability" so the picker can mark them as loop-only.
        Assert.Contains(userVars, v => v.Name == "Capability.Name" && v.Scope == "perCapability");
        Assert.Contains(userVars, v => v.Name == "Aws.AccountId" && v.Scope == "perCapability");
        Assert.Contains(userVars, v => v.Name == "Capability.Cost.30Days" && v.Scope == "perCapability");

        // Member.* is replaced by User.* in user campaigns and must not appear.
        Assert.DoesNotContain(userVars, v => v.Name == "Member.Email");
        Assert.DoesNotContain(userVars, v => v.Name == "Member.DisplayName");
    }

    [Fact]
    public void GetVariableDefinitions_Capability_OmitsUserVariables_AndScopesCostPerCapability()
    {
        var capVars = _sut.GetVariableDefinitions(EmailCampaignTargetType.Capability);

        Assert.Contains(capVars, v => v.Name == "Capability.Name" && v.Scope == "perCapability");
        Assert.Contains(capVars, v => v.Name == "Capability.Cost.7Days" && v.Scope == "perCapability");
        Assert.Contains(capVars, v => v.Name == "Member.Email" && v.Scope == "topLevel");
        Assert.DoesNotContain(capVars, v => v.Name == "User.Email");
        Assert.DoesNotContain(capVars, v => v.Name == "User.CapabilityCount");
    }
}

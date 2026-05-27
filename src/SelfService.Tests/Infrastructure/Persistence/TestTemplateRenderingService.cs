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
        int pendingMembershipApplicationCount = 0
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

    [Fact]
    public void RenderTemplate_RequirementScore_RendersValue()
    {
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "mandatory_tags",
                Value = 83.3,
                DisplayName = "Use of Mandatory Tags",
                HelpUrl = "https://wiki.example.com/tags",
            },
            new()
            {
                RequirementId = "external_secrets",
                Value = 100,
                DisplayName = "External Secrets Adoption",
            },
        };
        var context = CreateContext(requirementScores: scores);

        var result = _sut.RenderTemplate(
            "Tags: {{Requirement.mandatory_tags}}, Secrets: {{Requirement.external_secrets}}",
            context
        );

        Assert.Equal("Tags: 83, Secrets: 100", result);
    }

    [Fact]
    public void RenderTemplate_RequirementScore_DisplayName_Renders()
    {
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "mandatory_tags",
                Value = 80,
                DisplayName = "Use of Mandatory Tags",
            },
        };
        var context = CreateContext(requirementScores: scores);

        var result = _sut.RenderTemplate("{{Requirement.mandatory_tags.DisplayName}}", context);

        Assert.Equal("Use of Mandatory Tags", result);
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
        var context = CreateContext(requirementScores: new List<RequirementsMetric>());

        var result = _sut.RenderTemplate("{{Requirement.mandatory_tags}}", context);

        Assert.Equal("N/A", result);
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
    public void RenderTemplate_AllNewVariablesCombined()
    {
        var scores = new List<RequirementsMetric>
        {
            new()
            {
                RequirementId = "mandatory_tags",
                Value = 100,
                DisplayName = "Tags",
            },
        };
        var awsAccount = A.AwsAccount.Build();
        awsAccount.RegisterRealAwsAccount("999888777666", "role@dfds.com", DateTime.UtcNow);
        var resources = new List<AzureResource> { A.AzureResource.WithEnvironment("dev").Build() };
        var context = CreateContext(
            awsAccount: awsAccount,
            azureResources: resources,
            requirementScores: scores,
            pendingMembershipApplicationCount: 2
        );

        var result = _sut.RenderTemplate(
            "Tags={{Requirement.mandatory_tags}}, AWS={{Aws.AccountId}}, Azure={{Azure.ResourceCount}}, Apps={{MembershipApplications.PendingCount}}",
            context
        );

        Assert.Equal("Tags=100, AWS=999888777666, Azure=1, Apps=2", result);
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
}

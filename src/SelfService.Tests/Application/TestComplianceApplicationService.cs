using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Compliance;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;

namespace SelfService.Tests.Application;

public class TestComplianceApplicationService
{
    private const string AllTagsPresent = """
        {
            "dfds.cost.centre": "ti-platform",
            "dfds.businessCapability": "Platform",
            "dfds.env": "production",
            "dfds.data.classification": "internal",
            "dfds.service.criticality": "high",
            "dfds.service.availability": "99.9"
        }
        """;

    private const string SomeTagsMissing = """
        {
            "dfds.cost.centre": "ti-platform",
            "dfds.businessCapability": "Platform"
        }
        """;

    private const string EmptyMetadata = "{}";

    private static IAwsAccountRepository AwsAccountRepoWithK8sLinkFor(params CapabilityId[] capabilityIds)
    {
        var mock = new Mock<IAwsAccountRepository>();
        mock.Setup(r => r.FindBy(It.IsAny<CapabilityId>())).ReturnsAsync((AwsAccount?)null);
        var linkedAccounts = new List<AwsAccount>();
        foreach (var capId in capabilityIds)
        {
            var account = AwsAccount.RequestNew(capId, DateTime.UtcNow, "test@dfds.com");
            account.LinkKubernetesNamespace($"ns-{capId}", DateTime.UtcNow);
            linkedAccounts.Add(account);
            mock.Setup(r => r.FindBy(capId)).ReturnsAsync(account);
        }
        mock.Setup(r => r.GetByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(
                (IEnumerable<CapabilityId> ids) =>
                {
                    var idSet = ids.Select(i => i.ToString()).ToHashSet();
                    return linkedAccounts.Where(a => idSet.Contains(a.CapabilityId.ToString())).ToList();
                }
            );
        return mock.Object;
    }

    private static IMembershipRepository MembershipRepoWithCounts(
        params (CapabilityId capabilityId, int count)[] counts
    )
    {
        var mock = new Mock<IMembershipRepository>();
        var countMap = counts.ToDictionary(x => x.capabilityId, x => x.count);
        mock.Setup(r => r.GetMemberCountsByCapabilityIds(It.IsAny<IEnumerable<CapabilityId>>()))
            .ReturnsAsync(
                (IEnumerable<CapabilityId> ids) =>
                    ids.Distinct().Where(id => countMap.ContainsKey(id)).ToDictionary(id => id, id => countMap[id])
            );
        return mock.Object;
    }

    [Fact]
    public async Task GetCapabilityCompliance_AllTagsPresent_TagsCategoryCompliant()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var tagsCategory = result.Categories.First(c => c.CategoryName == "Tags");
        Assert.Equal(ComplianceStatus.Compliant, tagsCategory.Status);
        Assert.Equal(6, tagsCategory.Items.Count);
        Assert.All(tagsCategory.Items, item => Assert.Equal("present", item.Status));
    }

    [Fact]
    public async Task GetCapabilityCompliance_SomeTagsMissing_TagsCategoryNonCompliant()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(SomeTagsMissing).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var tagsCategory = result.Categories.First(c => c.CategoryName == "Tags");
        Assert.Equal(ComplianceStatus.NonCompliant, tagsCategory.Status);

        var presentItems = tagsCategory.Items.Where(i => i.Status == "present").ToList();
        var missingItems = tagsCategory.Items.Where(i => i.Status == "missing").ToList();
        Assert.Equal(2, presentItems.Count);
        Assert.Equal(4, missingItems.Count);
    }

    [Fact]
    public async Task GetCapabilityCompliance_EmptyMetadata_TagsCategoryNonCompliant()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(EmptyMetadata).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var tagsCategory = result.Categories.First(c => c.CategoryName == "Tags");
        Assert.Equal(ComplianceStatus.NonCompliant, tagsCategory.Status);
        Assert.All(tagsCategory.Items, item => Assert.Equal("missing", item.Status));
    }

    [Fact]
    public async Task GetCapabilityCompliance_NonExistentCapability_ThrowsEntityNotFoundException()
    {
        var capabilityId = CapabilityId.CreateFrom("non-existent");
        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync((Capability?)null);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        await Assert.ThrowsAsync<EntityNotFoundException<Capability>>(
            () => service.GetCapabilityCompliance(capabilityId)
        );
    }

    [Fact]
    public async Task GetCapabilityCompliance_Stub_ExternalSecretsIsUnknown()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(capabilityId))
            .Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var externalSecrets = result.Categories.First(c => c.CategoryName == "External Secrets");
        Assert.Equal(ComplianceStatus.Unknown, externalSecrets.Status);
    }

    [Fact]
    public async Task GetCapabilityCompliance_AllTagsPresent_OverallCompliant()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        // With stub: only Tags is evaluated (Compliant), rest are Unknown
        Assert.Equal(ComplianceStatus.Compliant, result.OverallStatus);
    }

    [Fact]
    public async Task GetCapabilityCompliance_TagsMissing_OverallNonCompliant()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(EmptyMetadata).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        Assert.Equal(ComplianceStatus.NonCompliant, result.OverallStatus);
    }

    [Fact]
    public async Task GetCapabilityCompliance_HasFiveCategoriesTotal_WhenKubernetesLinked()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(capabilityId))
            .Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        Assert.Equal(5, result.Categories.Count);
        Assert.Contains(result.Categories, c => c.CategoryName == "Tags");
        Assert.Contains(result.Categories, c => c.CategoryName == "External Secrets");
        Assert.Contains(result.Categories, c => c.CategoryName == "IRSA Mutual Trust");
        Assert.Contains(result.Categories, c => c.CategoryName == "Workload Liveness and Readiness Probes");
        Assert.Contains(result.Categories, c => c.CategoryName == "ECR pull policy");
    }

    [Fact]
    public async Task GetCapabilityCompliance_NoAwsAccount_OnlyTagsCategoryReturned()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        // Default builder uses an IAwsAccountRepository whose FindBy returns null.
        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        Assert.Single(result.Categories);
        Assert.Equal("Tags", result.Categories[0].CategoryName);
    }

    [Fact]
    public async Task GetCapabilityCompliance_AwsAccountWithoutKubernetesLink_OnlyTagsCategoryReturned()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();
        var unlinkedAccount = AwsAccount.RequestNew(capabilityId, DateTime.UtcNow, "test@dfds.com");
        // No call to LinkKubernetesNamespace — KubernetesLink stays Unlinked.

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var awsRepo = new Mock<IAwsAccountRepository>();
        awsRepo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(unlinkedAccount);

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(capabilityRepo.Object)
            .WithAwsAccountRepository(awsRepo.Object)
            .Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        Assert.Single(result.Categories);
        Assert.Equal("Tags", result.Categories[0].CategoryName);
    }

    [Fact]
    public async Task GetCostCentreCompliance_FiltersMatchingCapabilities()
    {
        var cap1 = A
            .Capability.WithId(CapabilityId.CreateFrom("cap-1"))
            .WithJsonMetadata(AllTagsPresent) // cost.centre = "ti-platform"
            .Build();
        var cap2 = A
            .Capability.WithId(CapabilityId.CreateFrom("cap-2"))
            .WithJsonMetadata("""{"dfds.cost.centre": "other-centre"}""")
            .Build();
        var cap3 = A
            .Capability.WithId(CapabilityId.CreateFrom("cap-3"))
            .WithJsonMetadata(AllTagsPresent) // cost.centre = "ti-platform"
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { cap1, cap2, cap3 });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCostCentreCompliance("ti-platform");

        Assert.Equal("ti-platform", result.CostCentre);
        Assert.Equal(2, result.TotalCapabilities);
    }

    [Fact]
    public async Task GetCostCentreCompliance_ExcludesDeletedCapabilities()
    {
        var activeCap = A.Capability.WithId(CapabilityId.CreateFrom("active")).WithJsonMetadata(AllTagsPresent).Build();
        var deletedCap = A
            .Capability.WithId(CapabilityId.CreateFrom("deleted"))
            .WithStatus(CapabilityStatusOptions.Deleted)
            .WithJsonMetadata(AllTagsPresent)
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { activeCap });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCostCentreCompliance("ti-platform");

        Assert.Equal(1, result.TotalCapabilities);
    }

    [Fact]
    public async Task GetCostCentreComplianceDetails_ReturnsPerCapabilityBreakdownWithMetadataPreserved()
    {
        const string customMetadata = """
            {
                "dfds.cost.centre": "ti-platform",
                "dfds.custom.team": "alpha"
            }
            """;

        var cap2Id = CapabilityId.CreateFrom("cap-2");
        var cap1 = A.Capability.WithId(CapabilityId.CreateFrom("cap-1")).WithJsonMetadata(AllTagsPresent).Build();
        var cap2 = A.Capability.WithId(cap2Id).WithJsonMetadata(customMetadata).Build();
        var cap3 = A
            .Capability.WithId(CapabilityId.CreateFrom("cap-3"))
            .WithJsonMetadata("""{"dfds.cost.centre": "other-centre"}""")
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { cap1, cap2, cap3 });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var details = await service.GetCostCentreComplianceDetails("ti-platform");

        Assert.Equal("ti-platform", details.CostCentre);
        Assert.Equal(2, details.TotalCapabilities);
        Assert.Equal(2, details.Capabilities.Count);

        var cap2Result = details.Capabilities.Single(c => c.CapabilityId == cap2Id.ToString());
        Assert.Equal(customMetadata, cap2Result.JsonMetadata);
        Assert.Contains(cap2Result.Categories, c => c.CategoryName == "Tags");

        // Aggregate counts must agree with the legacy method for the same input.
        var aggregate = await service.GetCostCentreCompliance("ti-platform");
        Assert.Equal(aggregate.TotalCapabilities, details.TotalCapabilities);
        Assert.Equal(aggregate.CompliantCount, details.CompliantCount);
        Assert.Equal(aggregate.NonCompliantCount, details.NonCompliantCount);
    }

    [Fact]
    public async Task GetCostCentreCompliance_K8sCategoryCountsOnlyReflectK8sCapabilities()
    {
        var k8sCapId = CapabilityId.CreateFrom("k8s-cap");
        var nonK8sCapId = CapabilityId.CreateFrom("non-k8s-cap");
        var k8sCap = A.Capability.WithId(k8sCapId).WithJsonMetadata(AllTagsPresent).Build();
        var nonK8sCap = A.Capability.WithId(nonK8sCapId).WithJsonMetadata(AllTagsPresent).Build();

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { k8sCap, nonK8sCap });

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(capabilityRepo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(k8sCapId))
            .Build();

        var result = await service.GetCostCentreCompliance("ti-platform");

        Assert.Equal(2, result.TotalCapabilities);
        foreach (
            var k8sCategoryName in new[]
            {
                "External Secrets",
                "IRSA Mutual Trust",
                "Workload Liveness and Readiness Probes",
                "ECR pull policy",
            }
        )
        {
            var breakdown = result.Categories.First(c => c.CategoryName == k8sCategoryName);
            Assert.True(
                breakdown.CompliantCount + breakdown.NonCompliantCount <= 1,
                $"K8s category '{k8sCategoryName}' should account for at most 1 capability (the K8s-linked one), "
                    + $"but counted {breakdown.CompliantCount + breakdown.NonCompliantCount}."
            );
        }
    }

    [Fact]
    public async Task GetCapabilityCompliance_Stub_IrsaMutualTrustIsUnknown()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(capabilityId))
            .Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var irsaCategory = result.Categories.First(c => c.CategoryName == "IRSA Mutual Trust");
        Assert.Equal(ComplianceStatus.Unknown, irsaCategory.Status);
    }

    [Fact]
    public async Task GetCapabilityCompliance_Stub_WorkloadProbesIsUnknown()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(capabilityId))
            .Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var probesCategory = result.Categories.First(c => c.CategoryName == "Workload Liveness and Readiness Probes");
        Assert.Equal(ComplianceStatus.Unknown, probesCategory.Status);
    }

    [Fact]
    public async Task GetCapabilityCompliance_Stub_EcrPullIsUnknown()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(capabilityId))
            .Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var ecrCategory = result.Categories.First(c => c.CategoryName == "ECR pull policy");
        Assert.Equal(ComplianceStatus.Unknown, ecrCategory.Status);
    }

    [Fact]
    public async Task GetCapabilityCompliance_TagWithEmptyValue_TreatedAsMissing()
    {
        var metadata = """
            {
                "dfds.cost.centre": "",
                "dfds.businessCapability": "Platform",
                "dfds.env": "production",
                "dfds.data.classification": "internal",
                "dfds.service.criticality": "high",
                "dfds.service.availability": "99.9"
            }
            """;
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(metadata).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        var tagsCategory = result.Categories.First(c => c.CategoryName == "Tags");
        Assert.Equal(ComplianceStatus.NonCompliant, tagsCategory.Status);

        var costCentreItem = tagsCategory.Items.First(i => i.Name == "dfds.cost.centre");
        Assert.Equal("missing", costCentreItem.Status);
    }

    [Fact]
    public async Task GetRogueCapabilitiesCompliance_FiltersCapabilitiesWithoutCostCentre()
    {
        var withCostCentre = A
            .Capability.WithId(CapabilityId.CreateFrom("cost-centre-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();
        var missingCostCentre = A
            .Capability.WithId(CapabilityId.CreateFrom("missing-cost-centre-cap"))
            .WithJsonMetadata("""{"dfds.businessCapability": "Platform"}""")
            .Build();
        var emptyCostCentre = A
            .Capability.WithId(CapabilityId.CreateFrom("empty-cost-centre-cap"))
            .WithJsonMetadata("""{"dfds.cost.centre": ""}""")
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { withCostCentre, missingCostCentre, emptyCostCentre });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetRogueCapabilitiesCompliance();

        Assert.Equal("rogue", result.CostCentre);
        Assert.Equal(2, result.TotalCapabilities);
    }

    [Fact]
    public async Task GetRogueCapabilitiesComplianceDetails_MatchesAggregateCounts()
    {
        var rogueCap = A
            .Capability.WithId(CapabilityId.CreateFrom("rogue-cap"))
            .WithJsonMetadata("""{"dfds.businessCapability": "Platform"}""")
            .Build();
        var nonRogueCap = A
            .Capability.WithId(CapabilityId.CreateFrom("non-rogue-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { rogueCap, nonRogueCap });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var details = await service.GetRogueCapabilitiesComplianceDetails();
        var aggregate = await service.GetRogueCapabilitiesCompliance();

        Assert.Equal("rogue", details.CostCentre);
        Assert.Single(details.Capabilities);
        Assert.Equal(aggregate.TotalCapabilities, details.TotalCapabilities);
        Assert.Equal(aggregate.CompliantCount, details.CompliantCount);
        Assert.Equal(aggregate.NonCompliantCount, details.NonCompliantCount);
    }

    [Fact]
    public async Task GetOrphanedCapabilitiesCompliance_FiltersCapabilitiesWithoutMembers()
    {
        var orphanedCap = A
            .Capability.WithId(CapabilityId.CreateFrom("orphaned-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();
        var nonOrphanedCap = A
            .Capability.WithId(CapabilityId.CreateFrom("non-orphaned-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { orphanedCap, nonOrphanedCap });

        var membershipRepo = MembershipRepoWithCounts((nonOrphanedCap.Id, 2));

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithMembershipRepository(membershipRepo)
            .Build();

        var result = await service.GetOrphanedCapabilitiesCompliance();

        Assert.Equal("orphaned", result.CostCentre);
        Assert.Equal(1, result.TotalCapabilities);
    }

    [Fact]
    public async Task GetOrphanedCapabilitiesComplianceDetails_MatchesAggregateCounts()
    {
        var orphanedCap = A
            .Capability.WithId(CapabilityId.CreateFrom("orphaned-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();
        var nonOrphanedCap = A
            .Capability.WithId(CapabilityId.CreateFrom("non-orphaned-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { orphanedCap, nonOrphanedCap });

        var membershipRepo = MembershipRepoWithCounts((nonOrphanedCap.Id, 1));

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(repo.Object)
            .WithMembershipRepository(membershipRepo)
            .Build();

        var details = await service.GetOrphanedCapabilitiesComplianceDetails();
        var aggregate = await service.GetOrphanedCapabilitiesCompliance();

        Assert.Equal("orphaned", details.CostCentre);
        Assert.Single(details.Capabilities);
        Assert.Equal(orphanedCap.Id.ToString(), details.Capabilities[0].CapabilityId);
        Assert.Equal(aggregate.TotalCapabilities, details.TotalCapabilities);
        Assert.Equal(aggregate.CompliantCount, details.CompliantCount);
        Assert.Equal(aggregate.NonCompliantCount, details.NonCompliantCount);
    }

    [Fact]
    public async Task GetRequirementsCompliance_ReturnsKnownRequirementsAndCounts()
    {
        var k8sCapId = CapabilityId.CreateFrom("k8s-cap");
        var nonK8sCapId = CapabilityId.CreateFrom("non-k8s-cap");
        var k8sCap = A.Capability.WithId(k8sCapId).WithJsonMetadata(AllTagsPresent).Build();
        var nonK8sCap = A.Capability.WithId(nonK8sCapId).WithJsonMetadata(AllTagsPresent).Build();

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { k8sCap, nonK8sCap });

        var service = A
            .ComplianceApplicationService.WithCapabilityRepository(capabilityRepo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(k8sCapId))
            .Build();

        var result = await service.GetRequirementsCompliance();

        Assert.Equal(5, result.Items.Count);

        var tags = result.Items.Single(i => i.RequirementId == "tags");
        Assert.Equal("Tags", tags.CategoryName);
        Assert.Equal(2, tags.TotalCapabilities);

        var externalSecrets = result.Items.Single(i => i.RequirementId == "external-secrets");
        Assert.Equal("External Secrets", externalSecrets.CategoryName);
        Assert.Equal(1, externalSecrets.TotalCapabilities);
    }

    [Fact]
    public async Task ComplianceResponses_IncludeOnlyMatchingNonCompliantItems()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<RequirementsDbContext>().UseSqlite(connection).Options;
        await using var db = new SqliteRequirementsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var firstId = CapabilityId.CreateFrom("first-cap");
        var secondId = CapabilityId.CreateFrom("second-cap");
        var first = A.Capability.WithId(firstId).WithJsonMetadata(AllTagsPresent).Build();
        var second = A.Capability.WithId(secondId).WithJsonMetadata("""{"dfds.cost.centre":"other"}""").Build();
        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(repo => repo.GetAllActive()).ReturnsAsync(new[] { first, second });
        capabilityRepo.Setup(repo => repo.FindBy(firstId)).ReturnsAsync(first);

        db.Metrics.AddRange(
            new RequirementsMetric
            {
                Name = "dfds_requirement_item",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "external_secrets",
                Measurement = "non_compliant",
                Type = "gauge",
                Value = 1,
                Item = "secret-b",
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_item",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "external_secrets",
                Measurement = "non_compliant",
                Type = "gauge",
                Value = 1,
                Item = "secret-a",
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_item",
                CapabilityRootId = secondId.ToString(),
                RequirementId = "irsa",
                Measurement = "non_compliant",
                Type = "gauge",
                Item = "other-account",
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_item",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "irsa",
                Measurement = "non_compliant",
                Type = "gauge",
                Item = "service-account",
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_item",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "mandatory_tags",
                Measurement = "non_compliant",
                Type = "gauge",
                Item = "dfds.service.criticality",
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_value",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "irsa",
                Measurement = "non_compliant_total",
                Value = 1,
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_value",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "external_secrets",
                Measurement = "non_compliant_total",
                Type = "gauge",
                Value = 0,
            },
            new RequirementsMetric
            {
                Name = "dfds_requirement_value",
                CapabilityRootId = firstId.ToString(),
                RequirementId = "external_secrets",
                Measurement = "compliant_total",
                Type = "gauge",
                Value = 1,
            }
        );
        await db.SaveChangesAsync();

        var service = A
            .ComplianceApplicationService.WithRequirementsDbContext(db)
            .WithCapabilityRepository(capabilityRepo.Object)
            .WithAwsAccountRepository(AwsAccountRepoWithK8sLinkFor(firstId))
            .Build();

        var requirements = await service.GetRequirementsCompliance();
        Assert.Equal(5, requirements.Items.Count);

        var costCentre = await service.GetCostCentreCompliance("ti-platform");
        var costCentreDetails = await service.GetCostCentreComplianceDetails("ti-platform");
        var costCentreCategory = costCentreDetails
            .Capabilities.Single()
            .Categories.Single(category => category.CategoryName == "External Secrets");
        Assert.Equal(new[] { "secret-a", "secret-b" }, costCentreCategory.NonCompliantItems);
        Assert.Equal(ComplianceStatus.Compliant, costCentreCategory.Status);

        var capability = await service.GetCapabilityCompliance(firstId);
        var capabilityCategory = capability.Categories.Single(category => category.CategoryName == "External Secrets");
        Assert.Equal(new[] { "secret-a", "secret-b" }, capabilityCategory.NonCompliantItems);
        Assert.Equal(ComplianceStatus.Compliant, capabilityCategory.Status);
        Assert.Equal(
            new[] { "service-account" },
            capability.Categories.Single(category => category.CategoryName == "IRSA Mutual Trust").NonCompliantItems
        );
        Assert.Equal(
            new[] { "dfds.service.criticality" },
            capability.Categories.Single(category => category.CategoryName == "Tags").NonCompliantItems
        );
        Assert.Equal(
            ComplianceStatus.NonCompliant,
            costCentreDetails
                .Capabilities.Single()
                .Categories.Single(category => category.CategoryName == "IRSA Mutual Trust")
                .Status
        );

        var requirementDetails = await service.GetRequirementComplianceDetails("external-secrets");
        var requirementCapability = Assert.Single(requirementDetails.Capabilities);
        Assert.Equal(new[] { "secret-a", "secret-b" }, requirementCapability.NonCompliantItems);

        var tagDetails = await service.GetRequirementComplianceDetails("tags");
        Assert.Equal(2, tagDetails.Capabilities.Count);
        Assert.Equal(
            new[] { "dfds.service.criticality" },
            tagDetails.Capabilities.Single(capability => capability.CapabilityId == firstId.ToString()).NonCompliantItems
        );
        Assert.Empty(tagDetails.Capabilities.Single(capability => capability.CapabilityId == secondId.ToString()).NonCompliantItems);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.Null(
            JsonNode.Parse(
                JsonSerializer.Serialize(RequirementsComplianceApiResource.From(requirements), jsonOptions)
            )!["nonCompliantItems"]
        );
        Assert.Null(
            JsonNode.Parse(JsonSerializer.Serialize(CostCentreComplianceApiResource.From(costCentre), jsonOptions))![
                "nonCompliantItems"
            ]
        );
        var costCentreJson = JsonNode.Parse(
            JsonSerializer.Serialize(CostCentreComplianceDetailsApiResource.From(costCentreDetails), jsonOptions)
        )!;
        Assert.Null(costCentreJson["nonCompliantItems"]);
        Assert.Equal(
            "secret-a",
            costCentreJson["capabilities"]![0]!["categories"]![1]!["nonCompliantItems"]![0]!.GetValue<string>()
        );
        var capabilityJson = JsonNode.Parse(
            JsonSerializer.Serialize(CapabilityComplianceApiResource.From(capability), jsonOptions)
        )!;
        Assert.Null(capabilityJson["nonCompliantItems"]);
        Assert.Equal("secret-a", capabilityJson["categories"]![1]!["nonCompliantItems"]![0]!.GetValue<string>());
        Assert.Equal("""["service-account"]""", capabilityJson["categories"]![2]!["nonCompliantItems"]!.ToJsonString());
        var requirementDetailsJson = JsonNode.Parse(
            JsonSerializer.Serialize(RequirementComplianceDetailsApiResource.From(requirementDetails), jsonOptions)
        )!;
        Assert.Equal(
            "secret-a",
            requirementDetailsJson["capabilities"]![0]!["nonCompliantItems"]![0]!.GetValue<string>()
        );
    }

    [Fact]
    public async Task GetRequirementComplianceDetails_Tags_ReturnsCapabilityLevelData()
    {
        var compliantCap = A
            .Capability.WithId(CapabilityId.CreateFrom("compliant-cap"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();
        var nonCompliantCap = A
            .Capability.WithId(CapabilityId.CreateFrom("non-compliant-cap"))
            .WithJsonMetadata(EmptyMetadata)
            .Build();

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { compliantCap, nonCompliantCap });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(capabilityRepo.Object).Build();

        var result = await service.GetRequirementComplianceDetails("tags");

        Assert.Equal("tags", result.RequirementId);
        Assert.Equal("Tags", result.CategoryName);
        Assert.Equal(2, result.TotalCapabilities);
        Assert.Equal(2, result.Capabilities.Count);
        Assert.Contains(result.Capabilities, c => c.Status == ComplianceStatus.Compliant);
        Assert.Contains(result.Capabilities, c => c.Status == ComplianceStatus.NonCompliant);
        Assert.All(result.Capabilities, c => Assert.NotEmpty(c.Items));
    }

    [Fact]
    public async Task GetRequirementComplianceDetails_UnknownRequirement_ThrowsKeyNotFoundException()
    {
        var capability = A.Capability.WithId(CapabilityId.CreateFrom("cap-1")).WithJsonMetadata(AllTagsPresent).Build();

        var capabilityRepo = new Mock<ICapabilityRepository>();
        capabilityRepo.Setup(r => r.GetAllActive()).ReturnsAsync(new[] { capability });

        var service = A.ComplianceApplicationService.WithCapabilityRepository(capabilityRepo.Object).Build();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetRequirementComplianceDetails("not-a-real-requirement")
        );
    }

    private sealed class SqliteRequirementsDbContext : RequirementsDbContext
    {
        public SqliteRequirementsDbContext(DbContextOptions<RequirementsDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder
                .Entity<RequirementsMetric>()
                .Property(metric => metric.Labels)
                .HasConversion(
                    labels => labels == null ? null : labels.ToJsonString(null),
                    json => json == null ? null : JsonNode.Parse(json, null, default)!.AsObject()
                )
                .HasColumnType("TEXT");
        }
    }
}

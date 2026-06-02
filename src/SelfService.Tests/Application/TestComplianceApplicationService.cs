using Moq;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

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

        var cap1 = A
            .Capability.WithId(CapabilityId.CreateFrom("cap-1"))
            .WithJsonMetadata(AllTagsPresent)
            .Build();
        var cap2 = A
            .Capability.WithId(CapabilityId.CreateFrom("cap-2"))
            .WithJsonMetadata(customMetadata)
            .Build();
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

        var cap2Result = details.Capabilities.Single(c => c.CapabilityId == "cap-2");
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
}

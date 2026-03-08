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

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

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
    public async Task GetCapabilityCompliance_HasFiveCategoriesTotal()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

        var result = await service.GetCapabilityCompliance(capabilityId);

        Assert.Equal(5, result.Categories.Count);
        Assert.Contains(result.Categories, c => c.CategoryName == "Tags");
        Assert.Contains(result.Categories, c => c.CategoryName == "External Secrets");
        Assert.Contains(result.Categories, c => c.CategoryName == "IRSA Mutual Trust");
        Assert.Contains(result.Categories, c => c.CategoryName == "Workload Liveness and Readiness Probes");
        Assert.Contains(result.Categories, c => c.CategoryName == "ECR pull policy");
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
    public async Task GetCapabilityCompliance_Stub_IrsaMutualTrustIsUnknown()
    {
        var capabilityId = CapabilityId.CreateFrom("test-cap");
        var capability = A.Capability.WithId(capabilityId).WithJsonMetadata(AllTagsPresent).Build();

        var repo = new Mock<ICapabilityRepository>();
        repo.Setup(r => r.FindBy(capabilityId)).ReturnsAsync(capability);

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

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

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

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

        var service = A.ComplianceApplicationService.WithCapabilityRepository(repo.Object).Build();

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

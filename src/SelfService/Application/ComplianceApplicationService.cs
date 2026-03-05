using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class ComplianceApplicationService : IComplianceApplicationService
{
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly RequirementsDbContext _requirementsDbContext;

    private static readonly string[] RequiredTags =
    {
        "dfds.cost.centre",
        "dfds.businessCapability",
        "dfds.env",
        "dfds.data.classification",
        "dfds.service.criticality",
        "dfds.service.availability",
    };

    private static readonly string[] PlaceholderCategories =
    {
        "Established mutual trust between AWS and k8s",
        "k8s - Readiness & liveness probes",
        "All accounts can pull from ECRs",
    };

    private static readonly string[] Categories =
    {
        "Tags",
        "External Secrets"
    };

    public ComplianceApplicationService(
        ICapabilityRepository capabilityRepository,
        RequirementsDbContext requirementsDbContext
    )
    {
        _capabilityRepository = capabilityRepository;
        _requirementsDbContext = requirementsDbContext;
    }

    public async Task<CapabilityComplianceResult> GetCapabilityCompliance(CapabilityId capabilityId)
    {
        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
        {
            throw new KeyNotFoundException($"Capability '{capabilityId}' not found.");
        }

        var categories = new List<ComplianceCategoryResult>();

        categories.Add(CheckTagCompliance(capability.JsonMetadata));
        categories.Add(await CheckExternalSecretsCompliance(capabilityId.ToString()));

        foreach (var placeholder in PlaceholderCategories)
        {
            categories.Add(
                new ComplianceCategoryResult
                {
                    CategoryName = placeholder,
                    Status = ComplianceStatus.Unknown,
                    Items = new(),
                }
            );
        }

        var overallStatus = DetermineOverallStatus(categories);
        var totalScore = categories.Count(c => c.Status == ComplianceStatus.Compliant);

        return new CapabilityComplianceResult
        {
            CapabilityId = capabilityId.ToString(),
            OverallStatus = overallStatus,
            TotalScore = totalScore,
            Categories = categories,
        };
    }

    public async Task<CostCentreComplianceResult> GetCostCentreCompliance(string costCentre)
    {
        var allCapabilities = await _capabilityRepository.GetAll();
        var activeCapabilities = allCapabilities.Where(c => c.Status == CapabilityStatusOptions.Active);

        var matchingCapabilities = activeCapabilities
            .Where(c =>
            {
                var cc = ExtractCostCentre(c.JsonMetadata);
                return string.Equals(cc, costCentre, StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        var capabilityResults = new List<CapabilityComplianceResult>();
        foreach (var cap in matchingCapabilities)
        {
            var categories = new List<ComplianceCategoryResult>();
            categories.Add(CheckTagCompliance(cap.JsonMetadata));
            categories.Add(await CheckExternalSecretsCompliance(cap.Id.ToString()));
            foreach (var placeholder in PlaceholderCategories)
            {
                categories.Add(
                    new ComplianceCategoryResult
                    {
                        CategoryName = placeholder,
                        Status = ComplianceStatus.Unknown,
                        Items = new(),
                    }
                );
            }
            capabilityResults.Add(
                new CapabilityComplianceResult
                {
                    CapabilityId = cap.Id.ToString(),
                    OverallStatus = DetermineOverallStatus(categories),
                    Categories = categories,
                }
            );
        }

        var allCategoryNames = Categories
            .Concat(PlaceholderCategories)
            .ToList();

        var categoryBreakdowns = allCategoryNames
            .Select(name => new CostCentreCategoryBreakdown
            {
                CategoryName = name,
                CompliantCount = capabilityResults.Count(r =>
                    r.Categories.Any(c => c.CategoryName == name && c.Status == ComplianceStatus.Compliant)
                ),
                NonCompliantCount = capabilityResults.Count(r =>
                    r.Categories.Any(c => c.CategoryName == name && c.Status == ComplianceStatus.NonCompliant)
                ),
            })
            .ToList();

        return new CostCentreComplianceResult
        {
            CostCentre = costCentre,
            TotalCapabilities = matchingCapabilities.Count,
            CompliantCount = capabilityResults.Count(r => r.OverallStatus == ComplianceStatus.Compliant),
            NonCompliantCount = capabilityResults.Count(r => r.OverallStatus == ComplianceStatus.NonCompliant),
            Categories = categoryBreakdowns,
        };
    }

    private ComplianceCategoryResult CheckTagCompliance(string? jsonMetadata)
    {
        var items = new List<ComplianceCategoryItem>();
        JsonObject? jsonObject = null;

        if (!string.IsNullOrEmpty(jsonMetadata))
        {
            jsonObject = JsonNode.Parse(jsonMetadata)?.AsObject();
        }

        foreach (var tag in RequiredTags)
        {
            var value = jsonObject?[tag]?.ToString();
            var isPresent = !string.IsNullOrEmpty(value);
            items.Add(
                new ComplianceCategoryItem
                {
                    Name = tag,
                    Status = isPresent ? "present" : "missing",
                    Detail = isPresent ? value : null,
                }
            );
        }

        var presentCount = items.Count(i => i.Status == "present");
        var score = (double)presentCount / items.Count * 100;

        return new ComplianceCategoryResult
        {
            CategoryName = "Tags",
            Status = presentCount == items.Count ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            Score = score,
            Items = items,
            Description = "Mandatory tags on a Capability level",
            HelpUrl = "https://wiki.dfds.cloud/en/playbooks/requirements/Mandatory-tags-for-capabilities",
            DisplayName = "Tags for Capabilities"
        };
    }

    private async Task<ComplianceCategoryResult> CheckExternalSecretsCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m => m.CapabilityRootId == capabilityId && m.RequirementId == "external_secrets")
            .ToListAsync();

        var secretsTotal = metrics.FirstOrDefault(m => m.Measurement == "secrets");
        var externalSecretsCount = metrics.FirstOrDefault(m => m.Measurement == "external_secrets");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (secretsTotal == null && externalSecretsCount == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "External Secrets",
                Status = ComplianceStatus.Unknown,
                Score = scoreMetric?.Value,
                HelpUrl = scoreMetric?.HelpUrl,
                DisplayName = scoreMetric?.DisplayName,
                Description = scoreMetric?.Description,
                Items = new(),
            };
        }

        var totalVal = secretsTotal?.Value ?? 0;
        var externalVal = externalSecretsCount?.Value ?? 0;
        var isCompliant = totalVal == externalVal;

        return new ComplianceCategoryResult
        {
            CategoryName = "External Secrets",
            Status = isCompliant ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            Score = scoreMetric?.Value,
            HelpUrl = scoreMetric?.HelpUrl,
            DisplayName = scoreMetric?.DisplayName,
            Description = scoreMetric?.Description,
            Items = new List<ComplianceCategoryItem>
            {
                new()
                {
                    Name = "secrets",
                    Status = totalVal.ToString("F0"),
                    Detail = "total secrets count",
                },
                new()
                {
                    Name = "external_secrets",
                    Status = externalVal.ToString("F0"),
                    Detail = "external secrets count",
                },
            },
        };
    }

    private static ComplianceStatus DetermineOverallStatus(List<ComplianceCategoryResult> categories)
    {
        var evaluated = categories.Where(c => c.Status != ComplianceStatus.Unknown).ToList();
        if (evaluated.Count == 0)
            return ComplianceStatus.Unknown;
        return evaluated.All(c => c.Status == ComplianceStatus.Compliant)
            ? ComplianceStatus.Compliant
            : ComplianceStatus.NonCompliant;
    }

    private static string? ExtractCostCentre(string? jsonMetadata)
    {
        if (string.IsNullOrEmpty(jsonMetadata))
            return null;
        var jsonObject = JsonNode.Parse(jsonMetadata)?.AsObject();
        return jsonObject?["dfds.cost.centre"]?.ToString();
    }
}

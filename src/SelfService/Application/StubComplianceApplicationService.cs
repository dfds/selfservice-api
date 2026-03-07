using System.Text.Json.Nodes;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;

namespace SelfService.Application;

public class StubComplianceApplicationService : IComplianceApplicationService
{
    private readonly ICapabilityRepository _capabilityRepository;

    private static readonly string[] RequiredTags =
    {
        "dfds.cost.centre",
        "dfds.businessCapability",
        "dfds.env",
        "dfds.data.classification",
        "dfds.service.criticality",
        "dfds.service.availability",
    };

    private static readonly string[] PlaceholderCategories = Array.Empty<string>();

    public StubComplianceApplicationService(ICapabilityRepository capabilityRepository)
    {
        _capabilityRepository = capabilityRepository;
    }

    public async Task<CapabilityComplianceResult> GetCapabilityCompliance(CapabilityId capabilityId)
    {
        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
        {
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        var categories = new List<ComplianceCategoryResult>();

        categories.Add(CheckTagCompliance(capability.JsonMetadata));

        // External Secrets unknown without requirements DB
        categories.Add(
            new ComplianceCategoryResult
            {
                CategoryName = "External Secrets",
                Status = ComplianceStatus.Unknown,
                Items = new(),
            }
        );

        // IRSA Mutual Trust unknown without requirements DB
        categories.Add(
            new ComplianceCategoryResult
            {
                CategoryName = "IRSA Mutual Trust",
                Status = ComplianceStatus.Unknown,
                Items = new(),
            }
        );

        // Workload Liveness and Readiness Probes unknown without requirements DB
        categories.Add(
            new ComplianceCategoryResult
            {
                CategoryName = "Workload Liveness and Readiness Probes",
                Status = ComplianceStatus.Unknown,
                Items = new(),
            }
        );

        // ECR pull policy unknown without requirements DB
        categories.Add(
            new ComplianceCategoryResult
            {
                CategoryName = "ECR pull policy",
                Status = ComplianceStatus.Unknown,
                Items = new(),
            }
        );

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

        var evaluated = categories.Where(c => c.Status != ComplianceStatus.Unknown).ToList();
        var overallStatus =
            evaluated.Count == 0 ? ComplianceStatus.Unknown
            : evaluated.All(c => c.Status == ComplianceStatus.Compliant) ? ComplianceStatus.Compliant
            : ComplianceStatus.NonCompliant;

        return new CapabilityComplianceResult
        {
            CapabilityId = capabilityId.ToString(),
            OverallStatus = overallStatus,
            TotalScore = categories.Count(c => c.Status == ComplianceStatus.Compliant),
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
            var result = await GetCapabilityCompliance(cap.Id);
            capabilityResults.Add(result);
        }

        var allCategoryNames = new[] { "Tags", "External Secrets", "IRSA Mutual Trust", "Workload Liveness and Readiness Probes", "ECR pull policy" }
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

    private static ComplianceCategoryResult CheckTagCompliance(string? jsonMetadata)
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
        };
    }

    private static string? ExtractCostCentre(string? jsonMetadata)
    {
        if (string.IsNullOrEmpty(jsonMetadata))
            return null;
        var jsonObject = JsonNode.Parse(jsonMetadata)?.AsObject();
        return jsonObject?["dfds.cost.centre"]?.ToString();
    }
}

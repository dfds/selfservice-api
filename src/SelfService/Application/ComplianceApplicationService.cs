using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;

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

    private static readonly string[] PlaceholderCategories = Array.Empty<string>();

    private static readonly string[] Categories =
    {
        "Tags",
        "External Secrets",
        "IRSA Mutual Trust",
        "Workload Liveness and Readiness Probes",
        "ECR pull policy",
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
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        var categories = new List<ComplianceCategoryResult>();

        categories.Add(CheckTagCompliance(capability.JsonMetadata));
        categories.Add(await CheckExternalSecretsCompliance(capabilityId.ToString()));
        categories.Add(await CheckIrsaMutualTrustCompliance(capabilityId.ToString()));
        categories.Add(await CheckK8sProbesCompliance(capabilityId.ToString()));
        categories.Add(await CheckEcrPullCompliance(capabilityId.ToString()));

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
        var activeCapabilities = await _capabilityRepository.GetAllActive();

        var matchingCapabilities = activeCapabilities
            .Where(c =>
                string.Equals(ExtractCostCentre(c.JsonMetadata), costCentre, StringComparison.OrdinalIgnoreCase)
            )
            .ToList();

        var capabilityIds = matchingCapabilities.Select(c => c.Id.ToString()).ToList();
        var allMetrics = await _requirementsDbContext
            .Metrics.Where(m => capabilityIds.Contains(m.CapabilityRootId))
            .ToListAsync();

        var metricMap = allMetrics
            .GroupBy(m => m.CapabilityRootId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(m => m.RequirementId).ToDictionary(rg => rg.Key, rg => rg.ToList())
            );

        var capabilityResults = matchingCapabilities
            .Select(cap =>
            {
                var capMetrics = metricMap.GetValueOrDefault(cap.Id.ToString(), new());
                var categories = new List<ComplianceCategoryResult>
                {
                    CheckTagCompliance(cap.JsonMetadata),
                    CheckExternalSecretsCompliance(capMetrics.GetValueOrDefault("external_secrets", new())),
                    CheckIrsaMutualTrustCompliance(capMetrics.GetValueOrDefault("irsa", new())),
                    CheckK8sProbesCompliance(capMetrics.GetValueOrDefault("k8s-probes", new())),
                    CheckEcrPullCompliance(capMetrics.GetValueOrDefault("ecr-pull", new())),
                };
                return new CapabilityComplianceResult
                {
                    CapabilityId = cap.Id.ToString(),
                    OverallStatus = DetermineOverallStatus(categories),
                    Categories = categories,
                };
            })
            .ToList();

        var allCategoryNames = Categories.Concat(PlaceholderCategories).ToList();

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
            DisplayName = "Tags for Capabilities",
        };
    }

    private async Task<ComplianceCategoryResult> CheckExternalSecretsCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m => m.CapabilityRootId == capabilityId && m.RequirementId == "external_secrets")
            .ToListAsync();
        return CheckExternalSecretsCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckExternalSecretsCompliance(List<RequirementsMetric> metrics)
    {
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

    private async Task<ComplianceCategoryResult> CheckIrsaMutualTrustCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m => m.CapabilityRootId == capabilityId && m.RequirementId == "irsa")
            .ToListAsync();
        return CheckIrsaMutualTrustCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckIrsaMutualTrustCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantSAs = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_service_accounts");
        var compliantSAs = metrics.FirstOrDefault(m => m.Measurement == "compliant_service_accounts");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantSAs == null && compliantSAs == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "IRSA Mutual Trust",
                Status = ComplianceStatus.Unknown,
                Score = scoreMetric?.Value,
                HelpUrl = scoreMetric?.HelpUrl,
                DisplayName = scoreMetric?.DisplayName,
                Description = scoreMetric?.Description,
                Items = new(),
            };
        }

        var nonCompliantVal = nonCompliantSAs?.Value ?? 0;
        var compliantVal = compliantSAs?.Value ?? 0;
        var isCompliant = nonCompliantVal == 0;

        return new ComplianceCategoryResult
        {
            CategoryName = "IRSA Mutual Trust",
            Status = isCompliant ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            Score = scoreMetric?.Value,
            HelpUrl = scoreMetric?.HelpUrl,
            DisplayName = scoreMetric?.DisplayName,
            Description = scoreMetric?.Description,
            Items = new List<ComplianceCategoryItem>
            {
                new()
                {
                    Name = "compliant_service_accounts",
                    Status = compliantVal.ToString("F0"),
                    Detail = "compliant service accounts count",
                },
                new()
                {
                    Name = "non_compliant_service_accounts",
                    Status = nonCompliantVal.ToString("F0"),
                    Detail = "non-compliant service accounts count",
                },
            },
        };
    }

    private async Task<ComplianceCategoryResult> CheckK8sProbesCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m => m.CapabilityRootId == capabilityId && m.RequirementId == "k8s-probes")
            .ToListAsync();
        return CheckK8sProbesCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckK8sProbesCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantWorkloads = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_workloads");
        var compliantWorkloads = metrics.FirstOrDefault(m => m.Measurement == "compliant_workloads");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantWorkloads == null && compliantWorkloads == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "Workload Liveness and Readiness Probes",
                Status = ComplianceStatus.Unknown,
                Score = scoreMetric?.Value,
                HelpUrl = scoreMetric?.HelpUrl,
                DisplayName = scoreMetric?.DisplayName,
                Description = scoreMetric?.Description,
                Items = new(),
            };
        }

        var nonCompliantVal = nonCompliantWorkloads?.Value ?? 0;
        var compliantVal = compliantWorkloads?.Value ?? 0;
        var isCompliant = nonCompliantVal == 0;

        return new ComplianceCategoryResult
        {
            CategoryName = "Workload Liveness and Readiness Probes",
            Status = isCompliant ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            Score = scoreMetric?.Value,
            HelpUrl = scoreMetric?.HelpUrl,
            DisplayName = scoreMetric?.DisplayName,
            Description = scoreMetric?.Description,
            Items = new List<ComplianceCategoryItem>
            {
                new()
                {
                    Name = "compliant_workloads",
                    Status = compliantVal.ToString("F0"),
                    Detail = "compliant workloads count",
                },
                new()
                {
                    Name = "non_compliant_workloads",
                    Status = nonCompliantVal.ToString("F0"),
                    Detail = "non-compliant workloads count",
                },
            },
        };
    }

    private async Task<ComplianceCategoryResult> CheckEcrPullCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m => m.CapabilityRootId == capabilityId && m.RequirementId == "ecr-pull")
            .ToListAsync();
        return CheckEcrPullCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckEcrPullCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantRepos = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_ecr_repos");
        var compliantRepos = metrics.FirstOrDefault(m => m.Measurement == "compliant_ecr_repos");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantRepos == null && compliantRepos == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "ECR pull policy",
                Status = ComplianceStatus.Unknown,
                Score = scoreMetric?.Value,
                HelpUrl = scoreMetric?.HelpUrl,
                DisplayName = scoreMetric?.DisplayName,
                Description = scoreMetric?.Description,
                Items = new(),
            };
        }

        var nonCompliantVal = nonCompliantRepos?.Value ?? 0;
        var compliantVal = compliantRepos?.Value ?? 0;
        var isCompliant = nonCompliantVal == 0;

        return new ComplianceCategoryResult
        {
            CategoryName = "ECR pull policy",
            Status = isCompliant ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            Score = scoreMetric?.Value,
            HelpUrl = scoreMetric?.HelpUrl,
            DisplayName = scoreMetric?.DisplayName,
            Description = scoreMetric?.Description,
            Items = new List<ComplianceCategoryItem>
            {
                new()
                {
                    Name = "compliant_ecr_repos",
                    Status = compliantVal.ToString("F0"),
                    Detail = "compliant ECR repositories count",
                },
                new()
                {
                    Name = "non_compliant_ecr_repos",
                    Status = nonCompliantVal.ToString("F0"),
                    Detail = "non-compliant ECR repositories count",
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

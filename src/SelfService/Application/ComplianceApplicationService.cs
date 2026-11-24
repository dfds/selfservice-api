using Microsoft.EntityFrameworkCore;
using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;
using SelfService.Infrastructure.Persistence.Models;
using static SelfService.Application.ComplianceAggregation;

namespace SelfService.Application;

public class ComplianceApplicationService : IComplianceApplicationService
{
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly RequirementsDbContext _requirementsDbContext;
    private readonly IComplianceSnapshotRepository _snapshotRepository;
    private readonly SystemTime _systemTime;

    private const string ItemMetricName = "dfds_requirement_item";

    public ComplianceApplicationService(
        ICapabilityRepository capabilityRepository,
        IAwsAccountRepository awsAccountRepository,
        IMembershipRepository membershipRepository,
        RequirementsDbContext requirementsDbContext,
        IComplianceSnapshotRepository snapshotRepository,
        SystemTime systemTime
    )
    {
        _capabilityRepository = capabilityRepository;
        _awsAccountRepository = awsAccountRepository;
        _membershipRepository = membershipRepository;
        _requirementsDbContext = requirementsDbContext;
        _snapshotRepository = snapshotRepository;
        _systemTime = systemTime;
    }

    public async Task<CapabilityComplianceResult> GetCapabilityCompliance(
        CapabilityId capabilityId,
        DateOnly? date = null
    )
    {
        if (date is { } snapshotDate)
        {
            return await GetSnapshotCapabilityCompliance(capabilityId, snapshotDate);
        }

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
        {
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        var categories = await ComputeLiveCategories(capability);

        return new CapabilityComplianceResult
        {
            CapabilityId = capabilityId.ToString(),
            OverallStatus = DetermineOverallStatus(categories),
            TotalScore = categories.Count(c => c.Status == ComplianceStatus.Compliant),
            Categories = categories,
        };
    }

    private async Task<List<ComplianceCategoryResult>> ComputeLiveCategories(Capability capability)
    {
        var capabilityId = capability.Id.ToString();
        var categories = new List<ComplianceCategoryResult> { CheckTagCompliance(capability.JsonMetadata) };

        if (await HasKubernetesContext(capability.Id))
        {
            categories.Add(await CheckExternalSecretsCompliance(capabilityId));
            categories.Add(await CheckIrsaMutualTrustCompliance(capabilityId));
            categories.Add(await CheckK8sProbesCompliance(capabilityId));
            categories.Add(await CheckEcrPullCompliance(capabilityId));
        }

        var itemMetrics = await _requirementsDbContext
            .Metrics.Where(metric => metric.CapabilityRootId == capabilityId && metric.Name == ItemMetricName)
            .ToListAsync();
        AddNonCompliantItems(categories, itemMetrics);
        return categories;
    }

    private async Task<CapabilityComplianceResult> GetSnapshotCapabilityCompliance(
        CapabilityId capabilityId,
        DateOnly date
    )
    {
        var entry = (
            await _snapshotRepository.GetCapabilityHistory(capabilityId.ToString(), date, date)
        ).FirstOrDefault();
        if (entry != null)
        {
            return ToCapabilityResult(entry.Row);
        }

        if ((await _snapshotRepository.GetSummaries(date, date)).Count == 0)
        {
            throw new ComplianceSnapshotNotFoundException(date);
        }

        throw EntityNotFoundException<Capability>.UsingId(capabilityId);
    }

    private async Task<bool> HasKubernetesContext(CapabilityId capabilityId)
    {
        var awsAccount = await _awsAccountRepository.FindBy(capabilityId);
        return awsAccount?.KubernetesLink.LinkedAt is not null;
    }

    public async Task<CostCentreComplianceResult> GetCostCentreCompliance(string costCentre, DateOnly? date = null)
    {
        var details = await GetCostCentreComplianceDetails(costCentre, date);
        return ToCostCentreComplianceResult(details);
    }

    public async Task<CostCentreComplianceDetailsResult> GetCostCentreComplianceDetails(
        string costCentre,
        DateOnly? date = null
    )
    {
        return BuildDetails(costCentre, await LoadRows(date, InCostCentre(costCentre)));
    }

    public async Task<AllCostCentresComplianceResult> GetAllCostCentresCompliance(DateOnly? date = null)
    {
        return BuildAllCostCentres(await LoadRows(date, All));
    }

    public async Task<CostCentreComplianceResult> GetRogueCapabilitiesCompliance(DateOnly? date = null)
    {
        var details = await GetRogueCapabilitiesComplianceDetails(date);
        return ToCostCentreComplianceResult(details, ComplianceCostCentreKind.Rogue);
    }

    public async Task<CostCentreComplianceDetailsResult> GetRogueCapabilitiesComplianceDetails(DateOnly? date = null)
    {
        return BuildDetails(RogueLabel, await LoadRows(date, Rogue));
    }

    public async Task<CostCentreComplianceResult> GetOrphanedCapabilitiesCompliance(DateOnly? date = null)
    {
        var details = await GetOrphanedCapabilitiesComplianceDetails(date);
        return ToCostCentreComplianceResult(details, ComplianceCostCentreKind.Orphaned);
    }

    public async Task<CostCentreComplianceDetailsResult> GetOrphanedCapabilitiesComplianceDetails(DateOnly? date = null)
    {
        return BuildDetails(OrphanedLabel, await LoadRows(date, Orphaned));
    }

    public async Task<ComplianceSummaryResult> GetComplianceSummary(DateOnly? date = null)
    {
        return BuildSummary(await LoadRows(date, All));
    }

    public async Task<RequirementsComplianceResult> GetRequirementsCompliance(DateOnly? date = null)
    {
        return BuildRequirementsCompliance(await LoadRows(date, All));
    }

    public async Task<RequirementComplianceDetailsResult> GetRequirementComplianceDetails(
        string requirementId,
        DateOnly? date = null
    )
    {
        var definition = FindRequirementDefinition(requirementId);
        if (definition == null)
        {
            throw new KeyNotFoundException($"Unknown compliance requirement id '{requirementId}'.");
        }

        return BuildRequirementDetails(definition, await LoadRows(date, All));
    }

    public async Task<ComplianceCaptureResult> CaptureCurrent()
    {
        var capturedAt = _systemTime.Now;
        var date = DateOnly.FromDateTime(capturedAt);
        var rows = await ComputeLiveRows(All);

        return new ComplianceCaptureResult
        {
            Date = date,
            CapturedAt = capturedAt,
            Rows = rows,
            Summary = BuildDailySummary(date, capturedAt, rows),
        };
    }

    public async Task<ComplianceHistoryResult> GetHistory(DateOnly? from = null, DateOnly? to = null)
    {
        var oldestDate = await _snapshotRepository.GetOldestDate();
        if (oldestDate == null)
        {
            return new ComplianceHistoryResult();
        }

        var now = _systemTime.Now;
        var today = DateOnly.FromDateTime(now);
        var start = from ?? oldestDate.Value;
        var end = to ?? today;
        var items = await _snapshotRepository.GetSummaries(start, end);
        // The recorder captures hourly and the requirements DB refreshes every few minutes, so today's
        // snapshot can trail the live endpoints. Today is computed live instead.
        if (start <= today && today <= end)
        {
            var live = BuildDailySummary(today, now, await ComputeLiveRows(All));
            items = items.Where(x => x.Date != today).Append(live).ToList();
        }

        return new ComplianceHistoryResult { OldestDate = oldestDate, Items = items };
    }

    public async Task<CostCentreComplianceHistoryResult> GetCostCentreHistory(
        ComplianceCostCentreKind kind,
        string? costCentre,
        DateOnly? from = null,
        DateOnly? to = null
    )
    {
        var history = await GetHistory(from, to);
        return new CostCentreComplianceHistoryResult
        {
            OldestDate = history.OldestDate,
            Items = BuildCostCentreHistory(history.Items, kind, costCentre),
        };
    }

    public async Task<RequirementComplianceHistoryResult> GetRequirementHistory(
        string requirementId,
        DateOnly? from = null,
        DateOnly? to = null
    )
    {
        var definition =
            FindRequirementDefinition(requirementId)
            ?? throw new KeyNotFoundException($"Unknown compliance requirement id '{requirementId}'.");

        var history = await GetHistory(from, to);
        return new RequirementComplianceHistoryResult
        {
            OldestDate = history.OldestDate,
            RequirementId = definition.RequirementId,
            Items = BuildRequirementHistory(definition, history.Items),
        };
    }

    public async Task<List<CapabilityComplianceHistoryEntry>> GetCapabilityHistory(
        CapabilityId capabilityId,
        DateOnly from,
        DateOnly to
    )
    {
        var entries = await _snapshotRepository.GetCapabilityHistory(capabilityId.ToString(), from, to);
        var today = DateOnly.FromDateTime(_systemTime.Now);
        if (today < from || to < today)
        {
            return entries;
        }

        // Matches ComputeLiveRows, which covers active capabilities only.
        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null || capability.Status != CapabilityStatusOptions.Active)
        {
            return entries;
        }

        var categories = await ComputeLiveCategories(capability);
        var memberCounts = await _membershipRepository.GetMemberCountsByCapabilityIds(new[] { capability.Id });
        var live = new CapabilityComplianceRow(
            CapabilityId: capability.Id.ToString(),
            CapabilityName: capability.Name,
            JsonMetadata: capability.JsonMetadata,
            CostCentre: PlacementCostCentre(capability.JsonMetadata),
            MemberCount: memberCounts.GetValueOrDefault(capability.Id, 0),
            OverallStatus: DetermineOverallStatus(categories),
            Categories: categories
        );
        return entries.Where(x => x.Date != today).Append(new CapabilityComplianceHistoryEntry(today, live)).ToList();
    }

    private async Task<List<CapabilityComplianceRow>> LoadRows(DateOnly? date, Func<CapabilityPlacement, bool> filter)
    {
        if (date is not { } snapshotDate)
        {
            return await ComputeLiveRows(filter);
        }

        var rows = await _snapshotRepository.GetForDate(snapshotDate);
        if (rows.Count == 0)
        {
            throw new ComplianceSnapshotNotFoundException(snapshotDate);
        }

        return Filter(rows, filter);
    }

    // The filter runs on capabilities before any metric is read, so the requirements-DB query only covers
    // the capabilities that end up in the result.
    private async Task<List<CapabilityComplianceRow>> ComputeLiveRows(Func<CapabilityPlacement, bool> filter)
    {
        var activeCapabilities = (await _capabilityRepository.GetAllActive()).ToList();
        var memberCounts = await _membershipRepository.GetMemberCountsByCapabilityIds(
            activeCapabilities.Select(c => c.Id)
        );

        var matching = activeCapabilities
            .Select(c =>
                (
                    Capability: c,
                    Placement: new CapabilityPlacement(
                        PlacementCostCentre(c.JsonMetadata),
                        memberCounts.GetValueOrDefault(c.Id, 0)
                    )
                )
            )
            .Where(x => filter(x.Placement))
            .ToList();

        var capabilityIds = matching.Select(x => x.Capability.Id.ToString()).ToList();
        var requirementMetrics = await _requirementsDbContext
            .Metrics.Where(metric => capabilityIds.Contains(metric.CapabilityRootId))
            .ToListAsync();
        var aggregateMetrics = requirementMetrics
            .Where(metric => metric.Name != ItemMetricName && metric.RequirementId != null)
            .ToList();
        var itemMetrics = requirementMetrics.Where(metric => metric.Name == ItemMetricName).ToList();

        var metricMap = aggregateMetrics
            .GroupBy(m => m.CapabilityRootId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(m => m.RequirementId).ToDictionary(rg => rg.Key, rg => rg.ToList())
            );
        var itemMetricMap = itemMetrics.GroupBy(m => m.CapabilityRootId).ToDictionary(g => g.Key, g => g.ToList());

        var awsAccounts = await _awsAccountRepository.GetByCapabilityIds(matching.Select(x => x.Capability.Id));
        var k8sCapabilityIds = awsAccounts
            .Where(a => a.KubernetesLink.LinkedAt is not null)
            .Select(a => a.CapabilityId.ToString())
            .ToHashSet();

        return matching
            .Select(x =>
            {
                var cap = x.Capability;
                var capMetrics = metricMap.GetValueOrDefault(cap.Id.ToString(), new());
                var categories = new List<ComplianceCategoryResult> { CheckTagCompliance(cap.JsonMetadata) };

                if (k8sCapabilityIds.Contains(cap.Id.ToString()))
                {
                    categories.Add(
                        CheckExternalSecretsCompliance(capMetrics.GetValueOrDefault("external_secrets", new()))
                    );
                    categories.Add(CheckIrsaMutualTrustCompliance(capMetrics.GetValueOrDefault("irsa", new())));
                    categories.Add(CheckK8sProbesCompliance(capMetrics.GetValueOrDefault("k8s_probes", new())));
                    categories.Add(CheckEcrPullCompliance(capMetrics.GetValueOrDefault("ecr_pull", new())));
                }

                AddNonCompliantItems(categories, itemMetricMap.GetValueOrDefault(cap.Id.ToString(), new()));

                return new CapabilityComplianceRow(
                    CapabilityId: cap.Id.ToString(),
                    CapabilityName: cap.Name,
                    JsonMetadata: cap.JsonMetadata,
                    CostCentre: x.Placement.CostCentre,
                    MemberCount: x.Placement.MemberCount,
                    OverallStatus: DetermineOverallStatus(categories),
                    Categories: categories
                );
            })
            .ToList();
    }

    private static void AddNonCompliantItems(
        List<ComplianceCategoryResult> categories,
        IEnumerable<RequirementsMetric> metrics
    )
    {
        foreach (var category in categories)
        {
            var definition = RequirementDefinitions.FirstOrDefault(definition =>
                definition.CategoryName == category.CategoryName
            );
            category.NonCompliantItems =
                definition == null
                    ? new List<string>()
                    : metrics
                        .Where(metric =>
                            metric.Name == ItemMetricName
                            && metric.RequirementId == definition.MetricRequirementId
                            && metric.Measurement == "non_compliant"
                            && !string.IsNullOrEmpty(metric.Item)
                        )
                        .Select(metric => metric.Item!)
                        .OrderBy(item => item)
                        .ToList();
        }
    }

    private async Task<ComplianceCategoryResult> CheckExternalSecretsCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m =>
                m.CapabilityRootId == capabilityId && m.RequirementId == "external_secrets" && m.Name != ItemMetricName
            )
            .ToListAsync();
        return CheckExternalSecretsCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckExternalSecretsCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantWorkloads = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_total");
        var compliantWorkloads = metrics.FirstOrDefault(m => m.Measurement == "compliant_total");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantWorkloads == null && compliantWorkloads == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "External Secrets",
                Status = StatusFromScore(scoreMetric),
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
                    Name = "compliant_secrets",
                    Status = compliantVal.ToString("F0"),
                    Detail = "compliant external secrets count",
                },
                new()
                {
                    Name = "non_compliant_external_secrets",
                    Status = nonCompliantVal.ToString("F0"),
                    Detail = "non-compliant external secrets count",
                },
            },
        };
    }

    private async Task<ComplianceCategoryResult> CheckIrsaMutualTrustCompliance(string capabilityId)
    {
        var metrics = await _requirementsDbContext
            .Metrics.Where(m =>
                m.CapabilityRootId == capabilityId && m.RequirementId == "irsa" && m.Name != ItemMetricName
            )
            .ToListAsync();
        return CheckIrsaMutualTrustCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckIrsaMutualTrustCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantSAs = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_total");
        var compliantSAs = metrics.FirstOrDefault(m => m.Measurement == "compliant_total");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantSAs == null && compliantSAs == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "IRSA Mutual Trust",
                Status = StatusFromScore(scoreMetric),
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
            .Metrics.Where(m =>
                m.CapabilityRootId == capabilityId && m.RequirementId == "k8s_probes" && m.Name != ItemMetricName
            )
            .ToListAsync();
        return CheckK8sProbesCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckK8sProbesCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantWorkloads = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_total");
        var compliantWorkloads = metrics.FirstOrDefault(m => m.Measurement == "compliant_total");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantWorkloads == null && compliantWorkloads == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "Workload Liveness and Readiness Probes",
                Status = StatusFromScore(scoreMetric),
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
            .Metrics.Where(m =>
                m.CapabilityRootId == capabilityId && m.RequirementId == "ecr_pull" && m.Name != ItemMetricName
            )
            .ToListAsync();
        return CheckEcrPullCompliance(metrics);
    }

    private static ComplianceCategoryResult CheckEcrPullCompliance(List<RequirementsMetric> metrics)
    {
        var nonCompliantRepos = metrics.FirstOrDefault(m => m.Measurement == "non_compliant_total");
        var compliantRepos = metrics.FirstOrDefault(m => m.Measurement == "compliant_total");
        var scoreMetric = metrics.FirstOrDefault(m => m.Measurement == "score");

        if (nonCompliantRepos == null && compliantRepos == null)
        {
            return new ComplianceCategoryResult
            {
                CategoryName = "ECR pull policy",
                Status = StatusFromScore(scoreMetric),
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

    private static ComplianceStatus StatusFromScore(RequirementsMetric? scoreMetric)
    {
        if (scoreMetric == null)
            return ComplianceStatus.Unknown;
        return scoreMetric.Value >= 100 ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant;
    }
}

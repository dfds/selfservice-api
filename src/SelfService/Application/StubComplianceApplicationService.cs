using SelfService.Domain;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using static SelfService.Application.ComplianceAggregation;

namespace SelfService.Application;

/// <summary>
/// Used when no requirements DB is configured. Tags are evaluated; every metric-backed category is Unknown.
/// There is no snapshot history: any <c>date</c> throws <see cref="ComplianceSnapshotNotFoundException"/>.
/// </summary>
public class StubComplianceApplicationService : IComplianceApplicationService
{
    private readonly ICapabilityRepository _capabilityRepository;
    private readonly IAwsAccountRepository _awsAccountRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly SystemTime _systemTime;

    private static readonly string[] MetricCategoryNames =
    {
        "External Secrets",
        "IRSA Mutual Trust",
        "Workload Liveness and Readiness Probes",
        "ECR pull policy",
    };

    public StubComplianceApplicationService(
        ICapabilityRepository capabilityRepository,
        IAwsAccountRepository awsAccountRepository,
        IMembershipRepository membershipRepository,
        SystemTime systemTime
    )
    {
        _capabilityRepository = capabilityRepository;
        _awsAccountRepository = awsAccountRepository;
        _membershipRepository = membershipRepository;
        _systemTime = systemTime;
    }

    public async Task<CapabilityComplianceResult> GetCapabilityCompliance(
        CapabilityId capabilityId,
        DateOnly? date = null
    )
    {
        ThrowIfHistorical(date);

        var capability = await _capabilityRepository.FindBy(capabilityId);
        if (capability == null)
        {
            throw EntityNotFoundException<Capability>.UsingId(capabilityId);
        }

        var categories = BuildCategories(capability.JsonMetadata, await HasKubernetesContext(capabilityId));

        return new CapabilityComplianceResult
        {
            CapabilityId = capabilityId.ToString(),
            OverallStatus = DetermineOverallStatus(categories),
            TotalScore = categories.Count(c => c.Status == ComplianceStatus.Compliant),
            Categories = categories,
        };
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
        var rows = await LoadRows(null, All);

        return new ComplianceCaptureResult
        {
            Date = date,
            CapturedAt = capturedAt,
            Rows = rows,
            Summary = BuildDailySummary(date, capturedAt, rows),
        };
    }

    public Task<ComplianceHistoryResult> GetHistory(DateOnly? from = null, DateOnly? to = null)
    {
        return Task.FromResult(new ComplianceHistoryResult());
    }

    public Task<CostCentreComplianceHistoryResult> GetCostCentreHistory(
        ComplianceCostCentreKind kind,
        string? costCentre,
        DateOnly? from = null,
        DateOnly? to = null
    )
    {
        return Task.FromResult(new CostCentreComplianceHistoryResult());
    }

    public Task<RequirementComplianceHistoryResult> GetRequirementHistory(
        string requirementId,
        DateOnly? from = null,
        DateOnly? to = null
    )
    {
        var definition =
            FindRequirementDefinition(requirementId)
            ?? throw new KeyNotFoundException($"Unknown compliance requirement id '{requirementId}'.");
        return Task.FromResult(new RequirementComplianceHistoryResult { RequirementId = definition.RequirementId });
    }

    public Task<List<CapabilityComplianceHistoryEntry>> GetCapabilityHistory(
        CapabilityId capabilityId,
        DateOnly from,
        DateOnly to
    )
    {
        return Task.FromResult(new List<CapabilityComplianceHistoryEntry>());
    }

    private async Task<List<CapabilityComplianceRow>> LoadRows(DateOnly? date, Func<CapabilityPlacement, bool> filter)
    {
        ThrowIfHistorical(date);

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

        var awsAccounts = await _awsAccountRepository.GetByCapabilityIds(matching.Select(x => x.Capability.Id));
        var k8sCapabilityIds = awsAccounts
            .Where(a => a.KubernetesLink.LinkedAt is not null)
            .Select(a => a.CapabilityId.ToString())
            .ToHashSet();

        return matching
            .Select(x =>
            {
                var cap = x.Capability;
                var categories = BuildCategories(cap.JsonMetadata, k8sCapabilityIds.Contains(cap.Id.ToString()));

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

    private static void ThrowIfHistorical(DateOnly? date)
    {
        if (date is { } snapshotDate)
        {
            throw new ComplianceSnapshotNotFoundException(snapshotDate);
        }
    }

    private static List<ComplianceCategoryResult> BuildCategories(string? jsonMetadata, bool hasKubernetesContext)
    {
        var categories = new List<ComplianceCategoryResult> { CheckTagCompliance(jsonMetadata) };

        if (hasKubernetesContext)
        {
            categories.AddRange(
                MetricCategoryNames.Select(name => new ComplianceCategoryResult
                {
                    CategoryName = name,
                    Status = ComplianceStatus.Unknown,
                    Items = new(),
                })
            );
        }

        return categories;
    }

    private async Task<bool> HasKubernetesContext(CapabilityId capabilityId)
    {
        var awsAccount = await _awsAccountRepository.FindBy(capabilityId);
        return awsAccount?.KubernetesLink.LinkedAt is not null;
    }
}

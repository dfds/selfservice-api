using System.Text.Json.Nodes;
using SelfService.Domain.Services;

namespace SelfService.Application;

/// <summary>
/// Aggregations over <see cref="CapabilityComplianceRow"/> lists. Live and snapshot reads in both compliance
/// services go through these, so a historical response is computed exactly like a live one.
/// </summary>
internal static class ComplianceAggregation
{
    public const string RogueLabel = "rogue";
    public const string OrphanedLabel = "orphaned";
    public const string AllLabel = "all";

    public static readonly ComplianceRequirementDefinition[] RequirementDefinitions =
    {
        new("tags", "mandatory_tags", "Tags"),
        new("external-secrets", "external_secrets", "External Secrets"),
        new("irsa-mutual-trust", "irsa", "IRSA Mutual Trust"),
        new("workload-liveness-and-readiness-probes", "k8s_probes", "Workload Liveness and Readiness Probes"),
        new("ecr-pull-policy", "ecr_pull", "ECR pull policy"),
    };

    private static readonly string[] CategoryNames = RequirementDefinitions.Select(d => d.CategoryName).ToArray();

    public static Func<CapabilityPlacement, bool> All => _ => true;

    public static Func<CapabilityPlacement, bool> InCostCentre(string costCentre) =>
        p => string.Equals(p.CostCentre, costCentre, StringComparison.OrdinalIgnoreCase);

    public static Func<CapabilityPlacement, bool> Rogue => p => p.CostCentre == null;

    public static Func<CapabilityPlacement, bool> Orphaned => p => p.MemberCount == 0;

    public static ComplianceRequirementDefinition? FindRequirementDefinition(string requirementId) =>
        RequirementDefinitions.FirstOrDefault(definition =>
            string.Equals(definition.RequirementId, requirementId, StringComparison.OrdinalIgnoreCase)
        );

    public static string? ExtractCostCentre(string? jsonMetadata)
    {
        if (string.IsNullOrEmpty(jsonMetadata))
            return null;
        var jsonObject = JsonNode.Parse(jsonMetadata)?.AsObject();
        return jsonObject?["dfds.cost.centre"]?.ToString();
    }

    /// <summary>The cost centre stored on a row: null when missing or blank, which makes the capability rogue.</summary>
    public static string? PlacementCostCentre(string? jsonMetadata)
    {
        var costCentre = ExtractCostCentre(jsonMetadata);
        return string.IsNullOrWhiteSpace(costCentre) ? null : costCentre;
    }

    public static ComplianceStatus DetermineOverallStatus(List<ComplianceCategoryResult> categories)
    {
        var evaluated = categories.Where(c => c.Status != ComplianceStatus.Unknown).ToList();
        if (evaluated.Count == 0)
            return ComplianceStatus.Unknown;
        return evaluated.All(c => c.Status == ComplianceStatus.Compliant)
            ? ComplianceStatus.Compliant
            : ComplianceStatus.NonCompliant;
    }

    public static ComplianceCategoryResult CheckTagCompliance(string? jsonMetadata)
    {
        var evaluation = TagComplianceEvaluator.Evaluate(jsonMetadata);

        return new ComplianceCategoryResult
        {
            CategoryName = TagComplianceEvaluator.CategoryName,
            Status = evaluation.IsCompliant ? ComplianceStatus.Compliant : ComplianceStatus.NonCompliant,
            Score = evaluation.Score,
            Items = evaluation
                .Tags.Select(t => new ComplianceCategoryItem
                {
                    Name = t.Name,
                    Status = t.IsPresent ? "present" : "missing",
                    Detail = t.Value,
                })
                .ToList(),
            Description = TagComplianceEvaluator.Description,
            HelpUrl = TagComplianceEvaluator.HelpUrl,
            DisplayName = TagComplianceEvaluator.DisplayName,
        };
    }

    public static List<CapabilityComplianceRow> Filter(
        IEnumerable<CapabilityComplianceRow> rows,
        Func<CapabilityPlacement, bool> filter
    ) => rows.Where(row => filter(new CapabilityPlacement(row.CostCentre, row.MemberCount))).ToList();

    public static CapabilityComplianceResult ToCapabilityResult(CapabilityComplianceRow row) =>
        new()
        {
            CapabilityId = row.CapabilityId,
            OverallStatus = row.OverallStatus,
            TotalScore = row.Categories.Count(c => c.Status == ComplianceStatus.Compliant),
            Categories = row.Categories,
        };

    public static CostCentreComplianceDetailsResult BuildDetails(string label, List<CapabilityComplianceRow> rows)
    {
        var categoryBreakdowns = CategoryNames
            .Select(name => new CostCentreCategoryBreakdown
            {
                CategoryName = name,
                CompliantCount = rows.Count(r =>
                    r.Categories.Any(c => c.CategoryName == name && c.Status == ComplianceStatus.Compliant)
                ),
                NonCompliantCount = rows.Count(r =>
                    r.Categories.Any(c => c.CategoryName == name && c.Status == ComplianceStatus.NonCompliant)
                ),
            })
            .ToList();

        return new CostCentreComplianceDetailsResult
        {
            CostCentre = label,
            TotalCapabilities = rows.Count,
            CompliantCount = rows.Count(r => r.OverallStatus == ComplianceStatus.Compliant),
            NonCompliantCount = rows.Count(r => r.OverallStatus == ComplianceStatus.NonCompliant),
            UnknownCount = rows.Count(r => r.OverallStatus == ComplianceStatus.Unknown),
            Categories = categoryBreakdowns,
            Capabilities = rows.Select(r => new CostCentreCapabilityComplianceResult
                {
                    CapabilityId = r.CapabilityId,
                    CapabilityName = r.CapabilityName,
                    JsonMetadata = r.JsonMetadata,
                    OverallStatus = r.OverallStatus,
                    Categories = r.Categories,
                })
                .ToList(),
        };
    }

    public static CostCentreComplianceResult ToCostCentreComplianceResult(
        CostCentreComplianceDetailsResult details,
        ComplianceCostCentreKind kind = ComplianceCostCentreKind.CostCentre
    ) =>
        new()
        {
            CostCentre = details.CostCentre,
            Kind = kind,
            TotalCapabilities = details.TotalCapabilities,
            CompliantCount = details.CompliantCount,
            NonCompliantCount = details.NonCompliantCount,
            Categories = details.Categories,
        };

    public static ComplianceSummaryResult BuildSummary(List<CapabilityComplianceRow> rows) =>
        new()
        {
            TotalCapabilities = rows.Count,
            FullyCompliantCapabilities = rows.Count(r => r.OverallStatus == ComplianceStatus.Compliant),
            NonCompliantCapabilities = rows.Count(r => r.OverallStatus == ComplianceStatus.NonCompliant),
            UnknownCapabilities = rows.Count(r => r.OverallStatus == ComplianceStatus.Unknown),
        };

    /// <summary>
    /// Every cost centre, then rogue, then orphaned. Cost centres compare case-insensitively, matching
    /// <see cref="InCostCentre"/>; the label is the ordinal-smallest spelling. Orphaned capabilities also count
    /// in their cost centre or rogue group.
    /// </summary>
    public static List<CostCentreGroup> GroupByCostCentre(List<CapabilityComplianceRow> rows)
    {
        var costCentres = rows.Where(r => r.CostCentre != null)
            .GroupBy(r => r.CostCentre!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new CostCentreGroup(
                g.Select(r => r.CostCentre!).Min(StringComparer.Ordinal)!,
                ComplianceCostCentreKind.CostCentre,
                g.ToList()
            ))
            .OrderBy(g => g.Label, StringComparer.OrdinalIgnoreCase);

        return costCentres
            .Append(new CostCentreGroup(RogueLabel, ComplianceCostCentreKind.Rogue, Filter(rows, Rogue)))
            .Append(new CostCentreGroup(OrphanedLabel, ComplianceCostCentreKind.Orphaned, Filter(rows, Orphaned)))
            .ToList();
    }

    public static AllCostCentresComplianceResult BuildAllCostCentres(List<CapabilityComplianceRow> rows) =>
        new()
        {
            Items = GroupByCostCentre(rows)
                .Select(g => ToCostCentreComplianceResult(BuildDetails(g.Label, g.Rows), g.Kind))
                .ToList(),
        };

    public static RequirementsComplianceResult BuildRequirementsCompliance(List<CapabilityComplianceRow> rows) =>
        new() { Items = RequirementDefinitions.Select(d => BuildRequirementSummary(d, rows)).ToList() };

    public static RequirementComplianceSummaryResult BuildRequirementSummary(
        ComplianceRequirementDefinition definition,
        List<CapabilityComplianceRow> rows
    )
    {
        var matched = MatchCategory(definition, rows);
        var metadataSource = matched.Select(x => x.Category).FirstOrDefault();

        var byCostCentre = matched
            .GroupBy(x => ExtractCostCentre(x.Row.JsonMetadata))
            .OrderBy(g => g.Key)
            .Select(g => new RequirementByCostCentreResult
            {
                CostCentre = g.Key,
                TotalCapabilities = g.Count(),
                CompliantCount = g.Count(x => x.Category.Status == ComplianceStatus.Compliant),
                NonCompliantCount = g.Count(x => x.Category.Status == ComplianceStatus.NonCompliant),
                UnknownCount = g.Count(x => x.Category.Status == ComplianceStatus.Unknown),
            })
            .ToList();

        return new RequirementComplianceSummaryResult
        {
            RequirementId = definition.RequirementId,
            CategoryName = definition.CategoryName,
            DisplayName = metadataSource?.DisplayName ?? definition.CategoryName,
            Description = metadataSource?.Description,
            HelpUrl = metadataSource?.HelpUrl,
            TotalCapabilities = matched.Count,
            CompliantCount = matched.Count(x => x.Category.Status == ComplianceStatus.Compliant),
            NonCompliantCount = matched.Count(x => x.Category.Status == ComplianceStatus.NonCompliant),
            UnknownCount = matched.Count(x => x.Category.Status == ComplianceStatus.Unknown),
            ByCostCentre = byCostCentre,
        };
    }

    public static RequirementComplianceDetailsResult BuildRequirementDetails(
        ComplianceRequirementDefinition definition,
        List<CapabilityComplianceRow> rows
    )
    {
        var matched = MatchCategory(definition, rows);
        var metadataSource = matched.Select(x => x.Category).FirstOrDefault();

        return new RequirementComplianceDetailsResult
        {
            RequirementId = definition.RequirementId,
            CategoryName = definition.CategoryName,
            DisplayName = metadataSource?.DisplayName ?? definition.CategoryName,
            Description = metadataSource?.Description,
            HelpUrl = metadataSource?.HelpUrl,
            TotalCapabilities = matched.Count,
            CompliantCount = matched.Count(x => x.Category.Status == ComplianceStatus.Compliant),
            NonCompliantCount = matched.Count(x => x.Category.Status == ComplianceStatus.NonCompliant),
            UnknownCount = matched.Count(x => x.Category.Status == ComplianceStatus.Unknown),
            Capabilities = matched
                .Select(x => new RequirementCapabilityComplianceResult
                {
                    CapabilityId = x.Row.CapabilityId,
                    CapabilityName = x.Row.CapabilityName,
                    JsonMetadata = x.Row.JsonMetadata,
                    Status = x.Category.Status,
                    Score = x.Category.Score,
                    Items = x.Category.Items,
                    NonCompliantItems = x.Category.NonCompliantItems,
                })
                .ToList(),
        };
    }

    public static ComplianceDailySummaryResult BuildDailySummary(
        DateOnly date,
        DateTime capturedAt,
        List<CapabilityComplianceRow> rows
    )
    {
        var summary = BuildSummary(rows);

        return new ComplianceDailySummaryResult
        {
            Date = date,
            CapturedAt = capturedAt,
            TotalCapabilities = summary.TotalCapabilities,
            FullyCompliantCapabilities = summary.FullyCompliantCapabilities,
            NonCompliantCapabilities = summary.NonCompliantCapabilities,
            UnknownCapabilities = summary.UnknownCapabilities,
            Requirements = BuildRequirementTotals(rows),
            CostCentres = GroupByCostCentre(rows)
                .Select(g =>
                {
                    var details = BuildDetails(g.Label, g.Rows);
                    return new ComplianceCostCentreTotals
                    {
                        CostCentre = g.Kind == ComplianceCostCentreKind.CostCentre ? g.Label : null,
                        Kind = g.Kind,
                        TotalCapabilities = details.TotalCapabilities,
                        CompliantCount = details.CompliantCount,
                        NonCompliantCount = details.NonCompliantCount,
                        UnknownCount = details.UnknownCount,
                        Requirements = BuildRequirementTotals(g.Rows),
                    };
                })
                .ToList(),
        };
    }

    private static List<ComplianceRequirementTotals> BuildRequirementTotals(List<CapabilityComplianceRow> rows) =>
        RequirementDefinitions
            .Select(d => BuildRequirementSummary(d, rows))
            .Select(r => new ComplianceRequirementTotals
            {
                RequirementId = r.RequirementId,
                CategoryName = r.CategoryName,
                DisplayName = r.DisplayName,
                TotalCapabilities = r.TotalCapabilities,
                CompliantCount = r.CompliantCount,
                NonCompliantCount = r.NonCompliantCount,
                UnknownCount = r.UnknownCount,
            })
            .ToList();

    private static bool IsGroup(ComplianceCostCentreTotals totals, ComplianceCostCentreKind kind, string? costCentre) =>
        totals.Kind == kind
        && (
            kind != ComplianceCostCentreKind.CostCentre
            || string.Equals(totals.CostCentre, costCentre, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// One entry per summary that has the group. <paramref name="costCentre"/> is matched case-insensitively and
    /// ignored unless <paramref name="kind"/> is <see cref="ComplianceCostCentreKind.CostCentre"/>.
    /// </summary>
    public static List<CostCentreComplianceHistoryEntry> BuildCostCentreHistory(
        IEnumerable<ComplianceDailySummaryResult> summaries,
        ComplianceCostCentreKind kind,
        string? costCentre
    ) =>
        summaries
            .Select(s => (s.Date, Group: s.CostCentres.FirstOrDefault(c => IsGroup(c, kind, costCentre))))
            .Where(x => x.Group != null)
            .Select(x => new CostCentreComplianceHistoryEntry
            {
                Date = x.Date,
                TotalCapabilities = x.Group!.TotalCapabilities,
                CompliantCount = x.Group.CompliantCount,
                NonCompliantCount = x.Group.NonCompliantCount,
                UnknownCount = x.Group.UnknownCount,
                Requirements = x.Group.Requirements,
            })
            .ToList();

    /// <summary>
    /// One entry per summary that has the requirement. A group appears in <c>CostCentres</c> only when its stored
    /// totals include the requirement, so summaries captured before the per-group totals have none.
    /// </summary>
    public static List<RequirementComplianceHistoryEntry> BuildRequirementHistory(
        ComplianceRequirementDefinition definition,
        IEnumerable<ComplianceDailySummaryResult> summaries
    )
    {
        bool Matches(ComplianceRequirementTotals r) =>
            string.Equals(r.RequirementId, definition.RequirementId, StringComparison.OrdinalIgnoreCase);

        return summaries
            .Select(s => (Summary: s, Totals: s.Requirements.FirstOrDefault(Matches)))
            .Where(x => x.Totals != null)
            .Select(x => new RequirementComplianceHistoryEntry
            {
                Date = x.Summary.Date,
                Totals = x.Totals!,
                CostCentres = x
                    .Summary.CostCentres.Select(c => (Group: c, Totals: c.Requirements.FirstOrDefault(Matches)))
                    .Where(g => g.Totals != null)
                    .Select(g => new ComplianceCostCentreTotals
                    {
                        CostCentre = g.Group.CostCentre,
                        Kind = g.Group.Kind,
                        TotalCapabilities = g.Totals!.TotalCapabilities,
                        CompliantCount = g.Totals.CompliantCount,
                        NonCompliantCount = g.Totals.NonCompliantCount,
                        UnknownCount = g.Totals.UnknownCount,
                    })
                    .ToList(),
            })
            .ToList();
    }

    private static List<(CapabilityComplianceRow Row, ComplianceCategoryResult Category)> MatchCategory(
        ComplianceRequirementDefinition definition,
        List<CapabilityComplianceRow> rows
    ) =>
        rows.Select(row =>
                (Row: row, Category: row.Categories.FirstOrDefault(c => c.CategoryName == definition.CategoryName))
            )
            .Where(x => x.Category != null)
            .Select(x => (x.Row, x.Category!))
            .ToList();
}

internal sealed record ComplianceRequirementDefinition(
    string RequirementId,
    string MetricRequirementId,
    string CategoryName
);

/// <summary>The row fields that decide which cost-centre, rogue or orphaned bucket a capability falls into.</summary>
internal readonly record struct CapabilityPlacement(string? CostCentre, int MemberCount);

internal sealed record CostCentreGroup(string Label, ComplianceCostCentreKind Kind, List<CapabilityComplianceRow> Rows);

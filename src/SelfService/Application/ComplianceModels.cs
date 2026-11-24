namespace SelfService.Application;

public enum ComplianceStatus
{
    Compliant,
    NonCompliant,
    Unknown,
}

public class CapabilityComplianceResult
{
    public string CapabilityId { get; set; } = null!;
    public ComplianceStatus OverallStatus { get; set; }
    public int TotalScore { get; set; }
    public List<ComplianceCategoryResult> Categories { get; set; } = new();
}

public class ComplianceCategoryResult
{
    public string CategoryName { get; set; } = null!;
    public ComplianceStatus Status { get; set; }
    public double? Score { get; set; }
    public string? HelpUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<ComplianceCategoryItem> Items { get; set; } = new();
    public List<string> NonCompliantItems { get; set; } = new();
}

public class ComplianceCategoryItem
{
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Detail { get; set; }
}

public class CostCentreComplianceResult
{
    public string CostCentre { get; set; } = null!;
    public ComplianceCostCentreKind Kind { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public List<CostCentreCategoryBreakdown> Categories { get; set; } = new();
}

public class CostCentreCategoryBreakdown
{
    public string CategoryName { get; set; } = null!;
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
}

public class CostCentreComplianceDetailsResult
{
    public string CostCentre { get; set; } = null!;
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
    public List<CostCentreCategoryBreakdown> Categories { get; set; } = new();
    public List<CostCentreCapabilityComplianceResult> Capabilities { get; set; } = new();
}

public class CostCentreCapabilityComplianceResult
{
    public string CapabilityId { get; set; } = null!;
    public string CapabilityName { get; set; } = null!;
    public string? JsonMetadata { get; set; }
    public ComplianceStatus OverallStatus { get; set; }
    public List<ComplianceCategoryResult> Categories { get; set; } = new();
}

public class ComplianceSummaryResult
{
    public int TotalCapabilities { get; set; }
    public int FullyCompliantCapabilities { get; set; }
    public int NonCompliantCapabilities { get; set; }
    public int UnknownCapabilities { get; set; }
}

public class RequirementsComplianceResult
{
    public List<RequirementComplianceSummaryResult> Items { get; set; } = new();
}

public class RequirementByCostCentreResult
{
    public string? CostCentre { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
}

public class RequirementComplianceSummaryResult
{
    public string RequirementId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? HelpUrl { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
    public List<RequirementByCostCentreResult> ByCostCentre { get; set; } = new();
}

public class RequirementComplianceDetailsResult : RequirementComplianceSummaryResult
{
    public List<RequirementCapabilityComplianceResult> Capabilities { get; set; } = new();
}

public class RequirementCapabilityComplianceResult
{
    public string CapabilityId { get; set; } = null!;
    public string CapabilityName { get; set; } = null!;
    public string? JsonMetadata { get; set; }
    public ComplianceStatus Status { get; set; }
    public double? Score { get; set; }
    public List<ComplianceCategoryItem> Items { get; set; } = new();
    public List<string> NonCompliantItems { get; set; } = new();
}

/// <summary>
/// One capability's computed compliance plus the inputs that place it in a cost-centre, rogue or orphaned
/// bucket. Live computation and stored snapshots both produce this shape.
/// </summary>
public record CapabilityComplianceRow(
    string CapabilityId,
    string CapabilityName,
    string? JsonMetadata,
    string? CostCentre,
    int MemberCount,
    ComplianceStatus OverallStatus,
    List<ComplianceCategoryResult> Categories
);

public record CapabilityComplianceHistoryEntry(DateOnly Date, CapabilityComplianceRow Row);

public enum ComplianceCostCentreKind
{
    CostCentre,
    Rogue,
    Orphaned,
}

public class ComplianceDailySummaryResult
{
    public DateOnly Date { get; set; }
    public DateTime CapturedAt { get; set; }
    public int TotalCapabilities { get; set; }
    public int FullyCompliantCapabilities { get; set; }
    public int NonCompliantCapabilities { get; set; }
    public int UnknownCapabilities { get; set; }
    public List<ComplianceRequirementTotals> Requirements { get; set; } = new();
    public List<ComplianceCostCentreTotals> CostCentres { get; set; } = new();
}

public class ComplianceRequirementTotals
{
    public string RequirementId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;

    // Stored so a requirement removed from the requirements DB still renders in history.
    public string? DisplayName { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
}

public class ComplianceCostCentreTotals
{
    /// <summary>Null unless <see cref="Kind"/> is <see cref="ComplianceCostCentreKind.CostCentre"/>.</summary>
    public string? CostCentre { get; set; }
    public ComplianceCostCentreKind Kind { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }

    /// <summary>
    /// Per-requirement totals within the group. Stored with the summary but left out of <c>/compliance/history</c>;
    /// empty for summaries captured before it was added.
    /// </summary>
    public List<ComplianceRequirementTotals> Requirements { get; set; } = new();
}

public class AllCostCentresComplianceResult
{
    public List<CostCentreComplianceResult> Items { get; set; } = new();
}

/// <summary>Rows and summary from one live computation, ready for <c>IComplianceSnapshotRepository.ReplaceForDate</c>.</summary>
public class ComplianceCaptureResult
{
    public DateOnly Date { get; set; }
    public DateTime CapturedAt { get; set; }
    public List<CapabilityComplianceRow> Rows { get; set; } = new();
    public ComplianceDailySummaryResult Summary { get; set; } = null!;
}

public class ComplianceHistoryResult
{
    /// <summary>Null when no snapshot has been captured yet.</summary>
    public DateOnly? OldestDate { get; set; }
    public List<ComplianceDailySummaryResult> Items { get; set; } = new();
}

public class CostCentreComplianceHistoryResult
{
    /// <summary>Null when no snapshot has been captured yet.</summary>
    public DateOnly? OldestDate { get; set; }
    public List<CostCentreComplianceHistoryEntry> Items { get; set; } = new();
}

public class CostCentreComplianceHistoryEntry
{
    public DateOnly Date { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
    public List<ComplianceRequirementTotals> Requirements { get; set; } = new();
}

public class RequirementComplianceHistoryResult
{
    /// <summary>Null when no snapshot has been captured yet.</summary>
    public DateOnly? OldestDate { get; set; }
    public string RequirementId { get; set; } = null!;
    public List<RequirementComplianceHistoryEntry> Items { get; set; } = new();
}

public class RequirementComplianceHistoryEntry
{
    public DateOnly Date { get; set; }
    public ComplianceRequirementTotals Totals { get; set; } = null!;

    /// <summary>The requirement's totals per cost centre, rogue and orphaned group; their <c>Requirements</c> are empty.</summary>
    public List<ComplianceCostCentreTotals> CostCentres { get; set; } = new();
}

public static class ComplianceRetention
{
    public const int Days = 1100;

    /// <summary>The oldest date still retained on <paramref name="today"/>; anything earlier is pruned.</summary>
    public static DateOnly OldestRetained(DateOnly today) => today.AddDays(-Days);
}

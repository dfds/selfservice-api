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
}

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

using SelfService.Application;

namespace SelfService.Infrastructure.Api.Compliance;

public class CapabilityComplianceApiResource
{
    public string CapabilityId { get; set; } = null!;
    public string OverallStatus { get; set; } = null!;
    public int TotalScore { get; set; }
    public List<ComplianceCategoryApiResource> Categories { get; set; } = new();

    public static CapabilityComplianceApiResource From(CapabilityComplianceResult result)
    {
        return new CapabilityComplianceApiResource
        {
            CapabilityId = result.CapabilityId,
            OverallStatus = result.OverallStatus.ToString(),
            TotalScore = result.TotalScore,
            Categories = result.Categories.Select(ComplianceCategoryApiResource.From).ToList(),
        };
    }
}

public class ComplianceCategoryApiResource
{
    public string CategoryName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public double? Score { get; set; }
    public string? HelpUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public List<ComplianceCategoryItemApiResource> Items { get; set; } = new();

    public static ComplianceCategoryApiResource From(ComplianceCategoryResult result)
    {
        return new ComplianceCategoryApiResource
        {
            CategoryName = result.CategoryName,
            Status = result.Status.ToString(),
            Score = result.Score,
            HelpUrl = result.HelpUrl,
            DisplayName = result.DisplayName,
            Description = result.Description,
            Items = result.Items.Select(ComplianceCategoryItemApiResource.From).ToList(),
        };
    }
}

public class ComplianceCategoryItemApiResource
{
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Detail { get; set; }

    public static ComplianceCategoryItemApiResource From(ComplianceCategoryItem item)
    {
        return new ComplianceCategoryItemApiResource
        {
            Name = item.Name,
            Status = item.Status,
            Detail = item.Detail,
        };
    }
}

public class CostCentreComplianceApiResource
{
    public string CostCentre { get; set; } = null!;
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public List<CostCentreCategoryBreakdownApiResource> Categories { get; set; } = new();

    public static CostCentreComplianceApiResource From(CostCentreComplianceResult result)
    {
        return new CostCentreComplianceApiResource
        {
            CostCentre = result.CostCentre,
            TotalCapabilities = result.TotalCapabilities,
            CompliantCount = result.CompliantCount,
            NonCompliantCount = result.NonCompliantCount,
            Categories = result.Categories.Select(CostCentreCategoryBreakdownApiResource.From).ToList(),
        };
    }
}

public class CostCentreCategoryBreakdownApiResource
{
    public string CategoryName { get; set; } = null!;
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }

    public static CostCentreCategoryBreakdownApiResource From(CostCentreCategoryBreakdown breakdown)
    {
        return new CostCentreCategoryBreakdownApiResource
        {
            CategoryName = breakdown.CategoryName,
            CompliantCount = breakdown.CompliantCount,
            NonCompliantCount = breakdown.NonCompliantCount,
        };
    }
}

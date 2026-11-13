using SelfService.Application;

namespace SelfService.Infrastructure.Api.Compliance;

public class ComplianceSummaryApiResource
{
    public int TotalCapabilities { get; set; }
    public int FullyCompliantCapabilities { get; set; }
    public int NonCompliantCapabilities { get; set; }
    public int UnknownCapabilities { get; set; }

    public static ComplianceSummaryApiResource From(ComplianceSummaryResult result)
    {
        return new ComplianceSummaryApiResource
        {
            TotalCapabilities = result.TotalCapabilities,
            FullyCompliantCapabilities = result.FullyCompliantCapabilities,
            NonCompliantCapabilities = result.NonCompliantCapabilities,
            UnknownCapabilities = result.UnknownCapabilities,
        };
    }
}

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
    public List<string> NonCompliantItems { get; set; } = new();

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
            NonCompliantItems = result.NonCompliantItems,
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
    public ComplianceCostCentreKind Kind { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public List<CostCentreCategoryBreakdownApiResource> Categories { get; set; } = new();

    public static CostCentreComplianceApiResource From(CostCentreComplianceResult result)
    {
        return new CostCentreComplianceApiResource
        {
            CostCentre = result.CostCentre,
            Kind = result.Kind,
            TotalCapabilities = result.TotalCapabilities,
            CompliantCount = result.CompliantCount,
            NonCompliantCount = result.NonCompliantCount,
            Categories = result.Categories.Select(CostCentreCategoryBreakdownApiResource.From).ToList(),
        };
    }
}

public class AllCostCentresComplianceApiResource
{
    public List<CostCentreComplianceApiResource> Items { get; set; } = new();

    public static AllCostCentresComplianceApiResource From(AllCostCentresComplianceResult result)
    {
        return new AllCostCentresComplianceApiResource
        {
            Items = result.Items.Select(CostCentreComplianceApiResource.From).ToList(),
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

public class CostCentreComplianceDetailsApiResource
{
    public string CostCentre { get; set; } = null!;
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
    public List<CostCentreCategoryBreakdownApiResource> Categories { get; set; } = new();
    public List<CostCentreCapabilityComplianceApiResource> Capabilities { get; set; } = new();

    public static CostCentreComplianceDetailsApiResource From(CostCentreComplianceDetailsResult result)
    {
        return new CostCentreComplianceDetailsApiResource
        {
            CostCentre = result.CostCentre,
            TotalCapabilities = result.TotalCapabilities,
            CompliantCount = result.CompliantCount,
            NonCompliantCount = result.NonCompliantCount,
            UnknownCount = result.UnknownCount,
            Categories = result.Categories.Select(CostCentreCategoryBreakdownApiResource.From).ToList(),
            Capabilities = result.Capabilities.Select(CostCentreCapabilityComplianceApiResource.From).ToList(),
        };
    }
}

public class CostCentreCapabilityComplianceApiResource
{
    public string CapabilityId { get; set; } = null!;
    public string CapabilityName { get; set; } = null!;
    public string? JsonMetadata { get; set; }
    public string OverallStatus { get; set; } = null!;
    public List<ComplianceCategoryApiResource> Categories { get; set; } = new();

    public static CostCentreCapabilityComplianceApiResource From(CostCentreCapabilityComplianceResult result)
    {
        return new CostCentreCapabilityComplianceApiResource
        {
            CapabilityId = result.CapabilityId,
            CapabilityName = result.CapabilityName,
            JsonMetadata = result.JsonMetadata,
            OverallStatus = result.OverallStatus.ToString(),
            Categories = result.Categories.Select(ComplianceCategoryApiResource.From).ToList(),
        };
    }
}

public class RequirementsComplianceApiResource
{
    public List<RequirementComplianceSummaryApiResource> Items { get; set; } = new();

    public static RequirementsComplianceApiResource From(RequirementsComplianceResult result)
    {
        return new RequirementsComplianceApiResource
        {
            Items = result.Items.Select(RequirementComplianceSummaryApiResource.From).ToList(),
        };
    }
}

public class RequirementByCostCentreApiResource
{
    public string? CostCentre { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }

    public static RequirementByCostCentreApiResource From(RequirementByCostCentreResult result)
    {
        return new RequirementByCostCentreApiResource
        {
            CostCentre = result.CostCentre,
            TotalCapabilities = result.TotalCapabilities,
            CompliantCount = result.CompliantCount,
            NonCompliantCount = result.NonCompliantCount,
            UnknownCount = result.UnknownCount,
        };
    }
}

public class RequirementComplianceSummaryApiResource
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
    public List<RequirementByCostCentreApiResource> ByCostCentre { get; set; } = new();

    public static RequirementComplianceSummaryApiResource From(RequirementComplianceSummaryResult result)
    {
        return new RequirementComplianceSummaryApiResource
        {
            RequirementId = result.RequirementId,
            CategoryName = result.CategoryName,
            DisplayName = result.DisplayName,
            Description = result.Description,
            HelpUrl = result.HelpUrl,
            TotalCapabilities = result.TotalCapabilities,
            CompliantCount = result.CompliantCount,
            NonCompliantCount = result.NonCompliantCount,
            UnknownCount = result.UnknownCount,
            ByCostCentre = result.ByCostCentre.Select(RequirementByCostCentreApiResource.From).ToList(),
        };
    }
}

public class RequirementComplianceDetailsApiResource
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
    public List<RequirementCapabilityComplianceApiResource> Capabilities { get; set; } = new();

    public static RequirementComplianceDetailsApiResource From(RequirementComplianceDetailsResult result)
    {
        return new RequirementComplianceDetailsApiResource
        {
            RequirementId = result.RequirementId,
            CategoryName = result.CategoryName,
            DisplayName = result.DisplayName,
            Description = result.Description,
            HelpUrl = result.HelpUrl,
            TotalCapabilities = result.TotalCapabilities,
            CompliantCount = result.CompliantCount,
            NonCompliantCount = result.NonCompliantCount,
            UnknownCount = result.UnknownCount,
            Capabilities = result.Capabilities.Select(RequirementCapabilityComplianceApiResource.From).ToList(),
        };
    }
}

public class RequirementCapabilityComplianceApiResource
{
    public string CapabilityId { get; set; } = null!;
    public string CapabilityName { get; set; } = null!;
    public string? JsonMetadata { get; set; }
    public string Status { get; set; } = null!;
    public double? Score { get; set; }
    public List<ComplianceCategoryItemApiResource> Items { get; set; } = new();
    public List<string> NonCompliantItems { get; set; } = new();

    public static RequirementCapabilityComplianceApiResource From(RequirementCapabilityComplianceResult result)
    {
        return new RequirementCapabilityComplianceApiResource
        {
            CapabilityId = result.CapabilityId,
            CapabilityName = result.CapabilityName,
            JsonMetadata = result.JsonMetadata,
            Status = result.Status.ToString(),
            Score = result.Score,
            Items = result.Items.Select(ComplianceCategoryItemApiResource.From).ToList(),
            NonCompliantItems = result.NonCompliantItems,
        };
    }
}

public class ComplianceHistoryApiResource
{
    public DateOnly? OldestDate { get; set; }
    public List<ComplianceDailySummaryApiResource> Items { get; set; } = new();

    public static ComplianceHistoryApiResource From(ComplianceHistoryResult result)
    {
        return new ComplianceHistoryApiResource
        {
            OldestDate = result.OldestDate,
            Items = result.Items.Select(ComplianceDailySummaryApiResource.From).ToList(),
        };
    }
}

public class ComplianceDailySummaryApiResource
{
    public DateOnly Date { get; set; }
    public DateTime CapturedAt { get; set; }
    public int TotalCapabilities { get; set; }
    public int FullyCompliantCapabilities { get; set; }
    public int NonCompliantCapabilities { get; set; }
    public int UnknownCapabilities { get; set; }
    public List<ComplianceRequirementTotalsApiResource> Requirements { get; set; } = new();
    public List<ComplianceCostCentreTotalsApiResource> CostCentres { get; set; } = new();

    public static ComplianceDailySummaryApiResource From(ComplianceDailySummaryResult result)
    {
        return new ComplianceDailySummaryApiResource
        {
            Date = result.Date,
            CapturedAt = result.CapturedAt,
            TotalCapabilities = result.TotalCapabilities,
            FullyCompliantCapabilities = result.FullyCompliantCapabilities,
            NonCompliantCapabilities = result.NonCompliantCapabilities,
            UnknownCapabilities = result.UnknownCapabilities,
            Requirements = result.Requirements.Select(ComplianceRequirementTotalsApiResource.From).ToList(),
            CostCentres = result.CostCentres.Select(ComplianceCostCentreTotalsApiResource.From).ToList(),
        };
    }
}

public class ComplianceRequirementTotalsApiResource
{
    public string RequirementId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }

    public static ComplianceRequirementTotalsApiResource From(ComplianceRequirementTotals totals)
    {
        return new ComplianceRequirementTotalsApiResource
        {
            RequirementId = totals.RequirementId,
            CategoryName = totals.CategoryName,
            DisplayName = totals.DisplayName,
            TotalCapabilities = totals.TotalCapabilities,
            CompliantCount = totals.CompliantCount,
            NonCompliantCount = totals.NonCompliantCount,
            UnknownCount = totals.UnknownCount,
        };
    }
}

public class ComplianceCostCentreTotalsApiResource
{
    public string? CostCentre { get; set; }
    public ComplianceCostCentreKind Kind { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }

    public static ComplianceCostCentreTotalsApiResource From(ComplianceCostCentreTotals totals)
    {
        return new ComplianceCostCentreTotalsApiResource
        {
            CostCentre = totals.CostCentre,
            Kind = totals.Kind,
            TotalCapabilities = totals.TotalCapabilities,
            CompliantCount = totals.CompliantCount,
            NonCompliantCount = totals.NonCompliantCount,
            UnknownCount = totals.UnknownCount,
        };
    }
}

public class CostCentreComplianceHistoryApiResource
{
    public DateOnly? OldestDate { get; set; }
    public List<CostCentreComplianceHistoryEntryApiResource> Items { get; set; } = new();

    public static CostCentreComplianceHistoryApiResource From(CostCentreComplianceHistoryResult result)
    {
        return new CostCentreComplianceHistoryApiResource
        {
            OldestDate = result.OldestDate,
            Items = result.Items.Select(CostCentreComplianceHistoryEntryApiResource.From).ToList(),
        };
    }
}

public class CostCentreComplianceHistoryEntryApiResource
{
    public DateOnly Date { get; set; }
    public int TotalCapabilities { get; set; }
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int UnknownCount { get; set; }
    public List<ComplianceRequirementTotalsApiResource> Requirements { get; set; } = new();

    public static CostCentreComplianceHistoryEntryApiResource From(CostCentreComplianceHistoryEntry entry)
    {
        return new CostCentreComplianceHistoryEntryApiResource
        {
            Date = entry.Date,
            TotalCapabilities = entry.TotalCapabilities,
            CompliantCount = entry.CompliantCount,
            NonCompliantCount = entry.NonCompliantCount,
            UnknownCount = entry.UnknownCount,
            Requirements = entry.Requirements.Select(ComplianceRequirementTotalsApiResource.From).ToList(),
        };
    }
}

public class RequirementComplianceHistoryApiResource
{
    public DateOnly? OldestDate { get; set; }
    public string RequirementId { get; set; } = null!;
    public List<RequirementComplianceHistoryEntryApiResource> Items { get; set; } = new();

    public static RequirementComplianceHistoryApiResource From(RequirementComplianceHistoryResult result)
    {
        return new RequirementComplianceHistoryApiResource
        {
            OldestDate = result.OldestDate,
            RequirementId = result.RequirementId,
            Items = result.Items.Select(RequirementComplianceHistoryEntryApiResource.From).ToList(),
        };
    }
}

public class RequirementComplianceHistoryEntryApiResource
{
    public DateOnly Date { get; set; }
    public ComplianceRequirementTotalsApiResource Totals { get; set; } = null!;
    public List<ComplianceCostCentreTotalsApiResource> CostCentres { get; set; } = new();

    public static RequirementComplianceHistoryEntryApiResource From(RequirementComplianceHistoryEntry entry)
    {
        return new RequirementComplianceHistoryEntryApiResource
        {
            Date = entry.Date,
            Totals = ComplianceRequirementTotalsApiResource.From(entry.Totals),
            CostCentres = entry.CostCentres.Select(ComplianceCostCentreTotalsApiResource.From).ToList(),
        };
    }
}

public class CapabilityComplianceHistoryApiResource
{
    public List<CapabilityComplianceHistoryEntryApiResource> Items { get; set; } = new();

    public static CapabilityComplianceHistoryApiResource From(IEnumerable<CapabilityComplianceHistoryEntry> entries)
    {
        return new CapabilityComplianceHistoryApiResource
        {
            Items = entries.Select(CapabilityComplianceHistoryEntryApiResource.From).ToList(),
        };
    }
}

public class CapabilityComplianceHistoryEntryApiResource
{
    public DateOnly Date { get; set; }
    public string OverallStatus { get; set; } = null!;
    public int CompliantCategories { get; set; }
    public int TotalCategories { get; set; }
    public List<ComplianceCategoryStatusApiResource> Categories { get; set; } = new();

    public static CapabilityComplianceHistoryEntryApiResource From(CapabilityComplianceHistoryEntry entry)
    {
        var categories = entry.Row.Categories;
        return new CapabilityComplianceHistoryEntryApiResource
        {
            Date = entry.Date,
            OverallStatus = entry.Row.OverallStatus.ToString(),
            CompliantCategories = categories.Count(c => c.Status == ComplianceStatus.Compliant),
            TotalCategories = categories.Count,
            Categories = categories
                .Select(c => new ComplianceCategoryStatusApiResource
                {
                    CategoryName = c.CategoryName,
                    Status = c.Status.ToString(),
                    Score = c.Score,
                })
                .ToList(),
        };
    }
}

public class ComplianceCategoryStatusApiResource
{
    public string CategoryName { get; set; } = null!;
    public string Status { get; set; } = null!;

    /// <summary>0-100, as on /compliance/capabilities/{id}; null when the category could not be evaluated.</summary>
    public double? Score { get; set; }
}

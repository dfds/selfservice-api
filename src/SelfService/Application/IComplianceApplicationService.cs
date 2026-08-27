using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IComplianceApplicationService
{
    Task<ComplianceSummaryResult> GetComplianceSummary();
    Task<CapabilityComplianceResult> GetCapabilityCompliance(CapabilityId capabilityId);
    Task<CostCentreComplianceResult> GetCostCentreCompliance(string costCentre);
    Task<CostCentreComplianceDetailsResult> GetCostCentreComplianceDetails(string costCentre);
    Task<CostCentreComplianceResult> GetRogueCapabilitiesCompliance();
    Task<CostCentreComplianceDetailsResult> GetRogueCapabilitiesComplianceDetails();
    Task<CostCentreComplianceResult> GetOrphanedCapabilitiesCompliance();
    Task<CostCentreComplianceDetailsResult> GetOrphanedCapabilitiesComplianceDetails();
    Task<RequirementsComplianceResult> GetRequirementsCompliance();
    Task<RequirementComplianceDetailsResult> GetRequirementComplianceDetails(string requirementId);
}

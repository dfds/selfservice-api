using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IComplianceApplicationService
{
    Task<CapabilityComplianceResult> GetCapabilityCompliance(CapabilityId capabilityId);
    Task<CostCentreComplianceResult> GetCostCentreCompliance(string costCentre);
}

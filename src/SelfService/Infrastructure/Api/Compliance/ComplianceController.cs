using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Compliance;

[Route("compliance")]
[Produces("application/json")]
[ApiController]
public class ComplianceController : ControllerBase
{
    private readonly IComplianceApplicationService _complianceService;

    public ComplianceController(IComplianceApplicationService complianceService)
    {
        _complianceService = complianceService;
    }

    [HttpGet("capabilities/{id}")]
    public async Task<IActionResult> GetCapabilityCompliance([FromRoute] string id)
    {
        try
        {
            var capabilityId = CapabilityId.Parse(id);
            var result = await _complianceService.GetCapabilityCompliance(capabilityId);
            return Ok(CapabilityComplianceApiResource.From(result));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("cost-centres/{costCentre}")]
    public async Task<IActionResult> GetCostCentreCompliance([FromRoute] string costCentre)
    {
        var result = await _complianceService.GetCostCentreCompliance(costCentre);
        return Ok(CostCentreComplianceApiResource.From(result));
    }
}

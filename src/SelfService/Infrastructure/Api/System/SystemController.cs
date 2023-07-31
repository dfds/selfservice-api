using Microsoft.AspNetCore.Mvc;
using SelfService.Infrastructure.BackgroundJobs;

namespace SelfService.Infrastructure.Api.System;

[Route("system")]
[ApiController]
public class SystemController : ControllerBase
{
    private readonly TopVisitorsRepository _topVisitorsRepository;
    private readonly IAadAwsSyncCapabilityQuery _aadAwsSyncCapabilityQuery;

    public SystemController(
        TopVisitorsRepository topVisitorsRepository,
        IAadAwsSyncCapabilityQuery aadAwsSyncCapabilityQuery
    )
    {
        _topVisitorsRepository = topVisitorsRepository;
        _aadAwsSyncCapabilityQuery = aadAwsSyncCapabilityQuery;
    }

    [HttpGet("stats/topvisitors")]
    public IActionResult GetTopVisitors()
    {
        var visitorRecords = _topVisitorsRepository.GetAll();
        return Ok(
            new
            {
                Items = visitorRecords
                    .OrderBy(x => x.Rank)
                    .Select(
                        x =>
                            new
                            {
                                Id = x.Id.ToString(),
                                Name = x.Name,
                                Rank = x.Rank,
                            }
                    ),
            }
        );
    }

    [HttpGet("legacy/aad-aws-sync")]
    public async Task<IActionResult> GetCapabilitiesForAadAwsSync()
    {
        var capabilities = await _aadAwsSyncCapabilityQuery.GetCapabilities();

        return Ok(capabilities);
    }
}

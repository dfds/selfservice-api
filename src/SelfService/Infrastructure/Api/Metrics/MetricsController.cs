using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Metrics;

[Route("metrics")]
[Produces("application/json")]
[ApiController]
public class MetricsController : ControllerBase
{
    private readonly IPlatformDataApiRequesterService _platformDataApiRequesterService;

    public MetricsController(IPlatformDataApiRequesterService platformDataApiRequesterService)
    {
        _platformDataApiRequesterService = platformDataApiRequesterService;
    }

    [HttpGet("my-capability-costs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetMyCapabilitiesCosts([FromQuery] int daysWindow) // daysWindow == 30
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var myCapabilitiesMetrics = await _platformDataApiRequesterService.GetMyCapabilitiesCosts(userId);

            if (myCapabilitiesMetrics.Costs.Count > 0)
                return Ok(myCapabilitiesMetrics);
        }
        catch (PlatformDataApiUnavailableException e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails
                {
                    Title = "PlatformDataApi unreachable",
                    Detail = $"PlatformDataApi error: {e.Message}."
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetCapabilityCosts: {e.Message}." }
            );
        }

        return NotFound(
            new ProblemDetails()
            {
                Title = "Capability Costs not found",
                Detail = $"No Cost data found for any capability",
            }
        );
    }

    [HttpGet("my-capabilities-resources")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetMyCapabilitiesAwsResources()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var myCapabilitiesMetrics = await _platformDataApiRequesterService.GetMyCapabilitiesAwsResourceCounts(
                userId
            );
            return Ok(myCapabilitiesMetrics);
        }
        catch (PlatformDataApiUnavailableException e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails
                {
                    Title = "PlatformDataApi unreachable",
                    Detail = $"PlatformDataApi error: {e.Message}."
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetCapabilityCosts: {e.Message}." }
            );
        }
    }
}

using Amazon.ECR.Model;
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
    private readonly OutOfSyncECRInfo _outOfSyncEcrInfo;

    public MetricsController(
        IPlatformDataApiRequesterService platformDataApiRequesterService,
        OutOfSyncECRInfo outOfSyncEcrInfo
    )
    {
        _platformDataApiRequesterService = platformDataApiRequesterService;
        _outOfSyncEcrInfo = outOfSyncEcrInfo;
    }

    [HttpGet("my-capabilities-costs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetMyCapabilitiesCosts()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var myCapabilitiesCosts = await _platformDataApiRequesterService.GetMyCapabilitiesCosts(userId);

            if (myCapabilitiesCosts.Costs.Count > 0)
                return Ok(myCapabilitiesCosts);
        }
        catch (PlatformDataApiUnavailableException e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails
                {
                    Title = "PlatformDataApi unreachable",
                    Detail = $"PlatformDataApi error: {e.Message}."
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
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

    [HttpGet("out-of-sync-ecr-repos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public IActionResult GetOutOfSyncECRRepos()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            if (_outOfSyncEcrInfo.HasBeenSet)
            {
                return Ok(_outOfSyncEcrInfo.RepositoriesNotInAwsCount + _outOfSyncEcrInfo.RepositoriesNotInDbCount);
                //:} Ok(-1); //for debugging the grafana/prometheus
            }
        }
        catch (PlatformDataApiUnavailableException e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails
                {
                    Title = "PlatformDataApi unreachable",
                    Detail = $"PlatformDataApi error: {e.Message}."
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
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
            var myCapabilitiesResourceCounts =
                await _platformDataApiRequesterService.GetMyCapabilitiesAwsResourceCounts(userId);
            return Ok(myCapabilitiesResourceCounts);
        }
        catch (PlatformDataApiUnavailableException e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails
                {
                    Title = "PlatformDataApi unreachable",
                    Detail = $"PlatformDataApi error: {e.Message}."
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetCapabilityCosts: {e.Message}." }
            );
        }
    }
}

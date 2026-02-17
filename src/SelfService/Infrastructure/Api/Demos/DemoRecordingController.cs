using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Demos;

[Route("demos")]
[Produces("application/json")]
[ApiController]
public class DemoRecordingController : ControllerBase
{
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IDemoRecordingService _demoRecordingService;
    private readonly IDemoApplicationService _demoApplicationService;
    private readonly IAuthorizationService _authorizationService;

    public DemoRecordingController(
        ApiResourceFactory apiResourceFactory,
        IDemoRecordingService demoRecordingService,
        IDemoApplicationService demoApplicationService,
        IAuthorizationService authorizationService
    )
    {
        _apiResourceFactory = apiResourceFactory;
        _demoRecordingService = demoRecordingService;
        _demoApplicationService = demoApplicationService;
        _authorizationService = authorizationService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(IEnumerable<DemoRecording>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetDemos()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        var demos = await _demoRecordingService.GetAllDemoRecordings();

        return Ok(_apiResourceFactory.Convert(demos));
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(DemoRecording), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> CreateDemo([FromBody] DemoRecordingCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        var demo = new DemoRecording(
            id: new DemoRecordingId(),
            recordingDate: request.RecordingDate,
            title: request.Title!,
            description: request.Description!,
            recordingUrl: request.RecordingUrl!,
            slidesUrl: request.SlidesUrl ?? string.Empty,
            createdBy: userId,
            createdAt: DateTime.UtcNow
        );

        var createdDemo = await _demoRecordingService.AddDemoRecording(demo);

        return Ok(_apiResourceFactory.Convert(createdDemo));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DemoRecording), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetDemo(DemoRecordingId id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        var demo = await _demoRecordingService.GetDemoRecordingById(id);

        return Ok(_apiResourceFactory.Convert(demo));
    }

    [HttpPost("{id}")]
    [ProducesResponseType(typeof(DemoRecording), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> UpdateDemo(DemoRecordingId id, [FromBody] DemoRecordingUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        await _demoRecordingService.UpdateDemoRecording(id, request);

        var updatedDemo = await _demoRecordingService.GetDemoRecordingById(id);

        return Ok(_apiResourceFactory.Convert(updatedDemo));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DemoRecording), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> DeleteDemo(DemoRecordingId id)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        await _demoRecordingService.DeleteDemoRecording(id);

        return NoContent();
    }

    [HttpGet("signups")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetActiveSignups()
    {
        if (!User.TryGetUserId(out var principalId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        var isCloudEngineer = _authorizationService.CanSynchronizeAwsECRAndDatabaseECR(User.ToPortalUser());
        if (!isCloudEngineer)
            return Unauthorized();

        return Ok(_apiResourceFactory.Convert(await _demoApplicationService.GetActiveSignups()));
    }
}

using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.ReleaseNotes;

[Route("release-notes")]
[Produces("application/json")]
[ApiController]
public class ReleaseNotesController : ControllerBase
{
    private readonly IReleaseNoteService _releaseNoteService;
    private readonly IAuthorizationService _authorizationService;

    public ReleaseNotesController(
        IReleaseNoteService releaseNoteService,
        IAuthorizationService authorizationService
    )
    {
        _releaseNoteService = releaseNoteService;
        _authorizationService = authorizationService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(IEnumerable<ReleaseNote>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetReleaseNotes()
    {
        try
        {
            var releaseNotes = await _releaseNoteService.GetAllReleaseNotes();
            return Ok(releaseNotes);
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetReleaseNotes: {e.Message}." }
            );
        }
    }

    private bool IsValidRequest(NewReleaseNotesRequest request)
    {
        return !string.IsNullOrEmpty(request.Title) && !string.IsNullOrEmpty(request.Content);
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(ReleaseNote), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> CreateReleaseNote([FromBody] NewReleaseNotesRequest request)
    {
        try
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            if (!IsValidRequest(request))
            {
                return BadRequest(
                    new ProblemDetails
                    {
                        Title = "Invalid Request",
                        Detail = "Title and content must not be empty.",
                    }
                );
            }
            var title = request.Title?.Trim()!;
            var content = request.Content?.Trim()!;

            var newNote = await _releaseNoteService.AddReleaseNote(
                title,
                content,
                request.ReleaseDate,
                userId
            );
            return Ok(newNote);
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails
                {
                    Title = "Uncaught Exception",
                    Detail = $"CreateReleaseNote: {e.InnerException}.",
                }
            );
        }
    }

    [HttpGet("{id:required}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> ToggleReleaseNoteIsActive(string id)
    {
        if (!ReleaseNoteId.TryParse(id, out var parsedId))
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid Release Note Id",
                    Detail = $"{id} is not a valid release note id.",
                }
            );
        }

        try
        {
            await _releaseNoteService.ToggleIsActive(parsedId);
            return Ok();
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"ToggleReleaseNoteIsActive: {e.Message}." }
            );
        }
    }
}

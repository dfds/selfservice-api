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
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly IReleaseNoteService _releaseNoteService;
    private readonly IAuthorizationService _authorizationService;

    public ReleaseNotesController(
        ApiResourceFactory apiResourceFactory,
        IReleaseNoteService releaseNoteService,
        IAuthorizationService authorizationService
    )
    {
        _apiResourceFactory = apiResourceFactory;
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
            if (Request.Query.ContainsKey("includeDrafts"))
            {
                var portalUser = HttpContext.User.ToPortalUser();
                if (!_authorizationService.IsAuthorizedToListDraftReleaseNotes(portalUser))
                {
                    return Unauthorized();
                }
            }
            else
            {
                releaseNotes = releaseNotes.Where(r => r.IsActive);
            }
            releaseNotes = releaseNotes.OrderByDescending(r => r.ReleaseDate);
            return Ok(_apiResourceFactory.Convert(releaseNotes));
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

            var portalUser = HttpContext.User.ToPortalUser();
            if (!_authorizationService.IsAuthorizedToCreateReleaseNotes(portalUser))
            {
                return Unauthorized();
            }

            if (!IsValidRequest(request))
            {
                return BadRequest(
                    new ProblemDetails { Title = "Invalid Request", Detail = "Title and content must not be empty." }
                );
            }
            var title = request.Title?.Trim()!;

            var newNote = await _releaseNoteService.AddReleaseNote(
                title,
                request.Content!,
                request.ReleaseDate,
                userId,
                1,
                request.IsActive
            );
            return Ok(_apiResourceFactory.Convert(newNote));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"CreateReleaseNote: {e.InnerException}." }
            );
        }
    }

    [HttpPut("{id:required}")]
    [ProducesResponseType(typeof(ReleaseNote), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> UpdateReleaseNote([FromBody] NewReleaseNotesRequest request, string id)
    {
        try
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var portalUser = HttpContext.User.ToPortalUser();
            if (!_authorizationService.IsAuthorizedToUpdateReleaseNote(portalUser))
            {
                return Unauthorized();
            }

            if (!IsValidRequest(request))
            {
                return BadRequest(
                    new ProblemDetails { Title = "Invalid Request", Detail = "Title and content must not be empty." }
                );
            }
            var title = request.Title?.Trim()!;

            await _releaseNoteService.UpdateReleaseNote(
                ReleaseNoteId.Parse(id),
                title,
                request.Content!,
                request.ReleaseDate,
                portalUser.Id.ToString()
            );
            return NoContent();
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"UpdateReleaseNote: {e.InnerException}." }
            );
        }
    }

    [HttpGet("{id:required}")]
    [ProducesResponseType(typeof(ReleaseNote), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetReleaseNote(string id)
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
            var releaseNote = await _releaseNoteService.GetReleaseNote(parsedId);
            if (releaseNote == null)
            {
                return NotFound(
                    new ProblemDetails
                    {
                        Title = "Release Note Not Found",
                        Detail = $"Release note with id {id} does not exist.",
                    }
                );
            }
            return Ok(_apiResourceFactory.Convert(releaseNote));
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetReleaseNote: {e.Message}." }
            );
        }
    }

    [HttpPost("{id:required}/toggle-active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> ToggleIsActive(string id)
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

        var portalUser = HttpContext.User.ToPortalUser();
        if (!_authorizationService.IsAuthorizedToToggleReleaseNoteIsActive(portalUser))
        {
            return Unauthorized();
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

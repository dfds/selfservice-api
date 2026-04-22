using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.News;

[Route("news")]
[Produces("application/json")]
[ApiController]
public class NewsController : ControllerBase
{
    private readonly ApiResourceFactory _apiResourceFactory;
    private readonly INewsItemService _newsItemService;
    private readonly IAuthorizationService _authorizationService;

    public NewsController(
        ApiResourceFactory apiResourceFactory,
        INewsItemService newsItemService,
        IAuthorizationService authorizationService
    )
    {
        _apiResourceFactory = apiResourceFactory;
        _newsItemService = newsItemService;
        _authorizationService = authorizationService;
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(NewsItemsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetAllNews()
    {
        if (!User.TryGetUserId(out _))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        var newsItems = await _newsItemService.GetAllNewsItems();

        return Ok(_apiResourceFactory.Convert(newsItems));
    }

    [HttpGet("relevant")]
    [ProducesResponseType(typeof(NewsItemsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetRelevantNews()
    {
        var newsItems = await _newsItemService.GetRelevantNewsItems();

        return Ok(_apiResourceFactory.ConvertRelevantNewsItems(newsItems));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NewsItemApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetNewsItem(NewsItemId id)
    {
        if (!User.TryGetUserId(out _))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        try
        {
            var newsItem = await _newsItemService.GetNewsItemById(id);
            return Ok(_apiResourceFactory.Convert(newsItem));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"News item with id '{id}' was not found.",
                }
            );
        }
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(NewsItemApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> CreateNewsItem([FromBody] NewsItemCreateRequest request)
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

        if (!_authorizationService.CanCreateNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to create news items. Only cloud engineers can perform this action.",
                }
            );
        }

        var newsItem = new NewsItem(
            id: new NewsItemId(),
            title: request.Title!,
            body: request.Body!,
            dueDate: request.DueDate,
            isHighlighted: false,
            createdBy: userId,
            createdAt: DateTime.UtcNow
        );

        var created = await _newsItemService.CreateNewsItem(newsItem);

        return Ok(_apiResourceFactory.Convert(created));
    }

    [HttpPost("{id}")]
    [ProducesResponseType(typeof(NewsItemApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> UpdateNewsItem(NewsItemId id, [FromBody] NewsItemUpdateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!User.TryGetUserId(out _))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanUpdateNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to update news items. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            await _newsItemService.UpdateNewsItem(id, request);
            var updated = await _newsItemService.GetNewsItemById(id);
            return Ok(_apiResourceFactory.Convert(updated));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"News item with id '{id}' was not found.",
                }
            );
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> DeleteNewsItem(NewsItemId id)
    {
        if (!User.TryGetUserId(out _))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanDeleteNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to delete news items. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            await _newsItemService.DeleteNewsItem(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"News item with id '{id}' was not found.",
                }
            );
        }
    }

    [HttpPost("{id}/highlight")]
    [ProducesResponseType(typeof(NewsItemApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> HighlightNewsItem(NewsItemId id)
    {
        if (!User.TryGetUserId(out _))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanUpdateNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail = "You are not authorized to highlight news items. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            await _newsItemService.HighlightNewsItem(id);
            var updated = await _newsItemService.GetNewsItemById(id);
            return Ok(_apiResourceFactory.Convert(updated));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = $"News item with id '{id}' was not found.",
                }
            );
        }
    }
}

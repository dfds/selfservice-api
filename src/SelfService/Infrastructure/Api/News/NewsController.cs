using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
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
    private readonly IContentImageService _contentImageService;

    public NewsController(
        ApiResourceFactory apiResourceFactory,
        INewsItemService newsItemService,
        IAuthorizationService authorizationService,
        IContentImageService contentImageService
    )
    {
        _apiResourceFactory = apiResourceFactory;
        _newsItemService = newsItemService;
        _authorizationService = authorizationService;
        _contentImageService = contentImageService;
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
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        var newsItems = await _newsItemService.GetAllNewsItems();
        var resource = _apiResourceFactory.Convert(newsItems);
        foreach (var item in resource.NewsItems)
        {
            await ResolveImagesAsync(item);
        }

        return Ok(resource);
    }

    [HttpGet("relevant")]
    [ProducesResponseType(typeof(NewsItemsApiResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetRelevantNews()
    {
        var newsItems = await _newsItemService.GetRelevantNewsItems();
        var resource = _apiResourceFactory.ConvertRelevantNewsItems(newsItems);
        foreach (var item in resource.NewsItems)
        {
            await ResolveImagesAsync(item);
        }

        return Ok(resource);
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
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        try
        {
            var newsItem = await _newsItemService.GetNewsItemById(id);
            return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(newsItem)));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails { Title = "Not Found", Detail = $"News item with id '{id}' was not found." }
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
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanCreateNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail =
                        "You are not authorized to create news items. Only cloud engineers can perform this action.",
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
            createdAt: DateTime.UtcNow,
            frontpageSummary: request.FrontpageSummary
        );

        var created = await _newsItemService.CreateNewsItem(newsItem);

        return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(created)));
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
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanUpdateNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail =
                        "You are not authorized to update news items. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            if (request.Body is not null)
            {
                request.Body = await _contentImageService.RestoreReferencesAsync(
                    ContentImageOwnerType.NewsItem,
                    (Guid)id,
                    request.Body,
                    HttpContext.RequestAborted
                );
            }
            if (request.FrontpageSummary is not null)
            {
                request.FrontpageSummary = await _contentImageService.RestoreReferencesAsync(
                    ContentImageOwnerType.NewsItem,
                    (Guid)id,
                    request.FrontpageSummary,
                    HttpContext.RequestAborted
                );
            }

            await _newsItemService.UpdateNewsItem(id, request);
            var updated = await _newsItemService.GetNewsItemById(id);
            return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(updated)));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails { Title = "Not Found", Detail = $"News item with id '{id}' was not found." }
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
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanDeleteNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail =
                        "You are not authorized to delete news items. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            await _contentImageService.DeleteForOwnerAsync(
                ContentImageOwnerType.NewsItem,
                (Guid)id,
                HttpContext.RequestAborted
            );
            await _newsItemService.DeleteNewsItem(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails { Title = "Not Found", Detail = $"News item with id '{id}' was not found." }
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
                    Detail = $"Value \"{User.GetUserId()}\" is not a valid user id.",
                }
            );
        }

        if (!_authorizationService.CanUpdateNewsItem(User.ToPortalUser()))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Unauthorized",
                    Detail =
                        "You are not authorized to highlight news items. Only cloud engineers can perform this action.",
                }
            );
        }

        try
        {
            await _newsItemService.HighlightNewsItem(id);
            var updated = await _newsItemService.GetNewsItemById(id);
            return Ok(await ResolveImagesAsync(_apiResourceFactory.Convert(updated)));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(
                new ProblemDetails { Title = "Not Found", Detail = $"News item with id '{id}' was not found." }
            );
        }
    }

    private async Task<NewsItemApiResource> ResolveImagesAsync(NewsItemApiResource resource)
    {
        var bodyHasImages = ContentImageReference.FindIds(resource.Body).Count > 0;
        var summaryHasImages = !string.IsNullOrEmpty(resource.FrontpageSummary)
            && ContentImageReference.FindIds(resource.FrontpageSummary).Count > 0;
        if (!bodyHasImages && !summaryHasImages)
        {
            return resource;
        }

        Response.Headers.CacheControl = "private, no-store";
        if (bodyHasImages)
        {
            resource.Body = await _contentImageService.ResolveReferencesAsync(
                ContentImageOwnerType.NewsItem,
                (Guid)resource.Id,
                resource.Body,
                HttpContext.RequestAborted
            );
        }

        if (summaryHasImages)
        {
            resource.FrontpageSummary = await _contentImageService.ResolveReferencesAsync(
                ContentImageOwnerType.NewsItem,
                (Guid)resource.Id,
                resource.FrontpageSummary!,
                HttpContext.RequestAborted
            );
        }

        return resource;
    }
}

using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Images;

[Route("images")]
[Produces("application/json")]
[ApiController]
public sealed class ContentImagesController : ControllerBase
{
    private readonly IContentImageService _contentImageService;

    public ContentImagesController(IContentImageService contentImageService)
    {
        _contentImageService = contentImageService;
    }

    [HttpPost("{ownerType}/{ownerId:guid}")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ContentImageApiResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(
        string ownerType,
        Guid ownerId,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken
    )
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        PreventCachingSignedUrls();

        if (!Enum.TryParse<ContentImageOwnerType>(ownerType, true, out var parsedOwnerType) || !Enum.IsDefined(parsedOwnerType))
        {
            return BadRequest(new ProblemDetails { Title = "Invalid owner type", Detail = "Unknown content type." });
        }

        if (file is null)
        {
            return BadRequest(new ProblemDetails { Title = "Missing image", Detail = "A multipart file field is required." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var uploaded = await _contentImageService.UploadAsync(
                parsedOwnerType,
                ownerId,
                file.FileName,
                stream,
                file.Length,
                cancellationToken
            );
            var resource = ContentImageApiResource.From(uploaded);
            return Created(resource.Url, resource);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid image", Detail = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Title = "Content item not found", Detail = exception.Message });
        }
    }

    [HttpDelete("{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid imageId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        return await _contentImageService.DeleteAsync(imageId, cancellationToken) ? NoContent() : NotFound();
    }

    private bool IsAuthorized() => User.ToPortalUser().Roles.Any(role => role == UserRole.CloudEngineer);

    private void PreventCachingSignedUrls()
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
    }
}
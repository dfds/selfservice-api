using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.PrivateAssets;

[Route("private-assets")]
[Produces("application/json")]
[ApiController]
public class PrivateAssetController : ControllerBase
{
    private const long MaxImageSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".webp",
    };

    private static readonly HashSet<string> AllowedMimeTypes = AllowedImageExtensions
        .Select(extension =>
            extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => string.Empty,
            }
        )
        .Where(value => !string.IsNullOrEmpty(value))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private readonly IPrivateAssetApplicationService _privateAssetService;
    private readonly IAuthorizationService _authorizationService;

    public PrivateAssetController(
        IPrivateAssetApplicationService privateAssetService,
        IAuthorizationService authorizationService
    )
    {
        _privateAssetService = privateAssetService;
        _authorizationService = authorizationService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PrivateAssetUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] Guid ownerId,
        [FromForm, BindRequired] PrivateAssetOwnerType ownerType,
        [FromForm] string contentId,
        [FromForm, BindRequired] bool isInline,
        CancellationToken cancellationToken
    )
    {
        if (!_authorizationService.CanCreatePrivateAsset(User.ToPortalUser()))
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied",
                    Detail = "Only users with the private-assets:create permission may upload private assets.",
                }
            );

        if (file is null || file.Length == 0)
            return BadRequest(
                new ProblemDetails { Title = "Invalid Request", Detail = "An uploaded file is required." }
            );

        if (file.Length > MaxImageSizeBytes)
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = $"File size must be less than or equal to {MaxImageSizeBytes / (1024 * 1024)}MB.",
                }
            );

        if (string.IsNullOrWhiteSpace(file.FileName))
            return BadRequest(
                new ProblemDetails { Title = "Invalid Request", Detail = "The uploaded file must include a file name." }
            );

        var mimeType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType.Trim();
        if (!IsSupportedImageType(file.FileName, mimeType))
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "Only PNG, JPG/JPEG, GIF, and WebP images are allowed.",
                }
            );

        await using var stream = file.OpenReadStream();
        return await Execute(async () =>
        {
            var asset = await _privateAssetService.Upload(
                ownerId,
                ownerType,
                contentId,
                isInline,
                file.FileName,
                mimeType,
                file.Length,
                stream,
                User.ToPortalUser(),
                cancellationToken
            );
            return Ok(await ToResource(asset, cancellationToken));
        });
    }

    [HttpGet("{id}")]
    public Task<IActionResult> Get(string id, CancellationToken cancellationToken) =>
        Execute(async () =>
        {
            var asset = await _privateAssetService.Get(PrivateAssetId.Parse(id));
            return Ok(await ToResource(asset, cancellationToken));
        });

    [HttpGet]
    public Task<IActionResult> GetByOwner(
        [FromQuery] Guid ownerId,
        [FromQuery, BindRequired] PrivateAssetOwnerType ownerType,
        CancellationToken cancellationToken
    ) =>
        Execute(async () =>
        {
            var assets = await _privateAssetService.GetByOwner(ownerId, ownerType);
            var resources = new List<PrivateAssetUploadResponse>();
            foreach (var asset in assets)
                resources.Add(await ToResource(asset, cancellationToken));
            return Ok(resources);
        });

    private async Task<PrivateAssetUploadResponse> ToResource(
        PrivateAsset asset,
        CancellationToken cancellationToken
    ) =>
        new()
        {
            AssetId = asset.Id.ToString(),
            OwnerId = asset.OwnerId,
            OwnerType = asset.OwnerType,
            ContentId = asset.ContentId,
            IsInline = asset.IsInline,
            StorageKey = asset.StorageKey,
            FileName = asset.FileName,
            MimeType = asset.MimeType,
            SizeInBytes = asset.SizeInBytes,
            CreatedAt = asset.CreatedAt,
            Url = await _privateAssetService.GetPreviewUrl(asset, cancellationToken),
        };

    private async Task<IActionResult> Execute(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (EntityNotFoundException exception)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = exception.Message });
        }
        catch (FormatException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Id", Detail = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Request", Detail = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new ProblemDetails { Title = "Invalid Operation", Detail = exception.Message });
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Duplicate ContentId",
                    Detail = "ContentId must be unique within its owner.",
                }
            );
        }
    }

    private static bool IsSupportedImageType(string fileName, string mimeType)
    {
        var extension = Path.GetExtension(fileName);
        if (AllowedImageExtensions.Contains(extension))
            return true;

        return AllowedMimeTypes.Contains(mimeType);
    }
}

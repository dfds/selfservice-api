using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Images;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public sealed class ContentImageService : IContentImageService
{
    private static readonly Regex UrlPattern = new(
        @"https?://[^\s""'<>]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );
    private const long DefaultMaximumSizeBytes = 10 * 1024 * 1024;
    private const int DefaultPresignedUrlLifetimeSeconds = 900;
    private const int MaximumPresignedUrlLifetimeSeconds = 3600;
    private readonly SelfServiceDbContext _dbContext;
    private readonly IImageObjectStorage _storage;
    private readonly long _maximumSizeBytes;
    private readonly TimeSpan _presignedUrlLifetime;
    private readonly ILogger<ContentImageService> _logger;

    public ContentImageService(
        SelfServiceDbContext dbContext,
        IImageObjectStorage storage,
        IConfiguration configuration,
        ILogger<ContentImageService> logger
    )
    {
        _dbContext = dbContext;
        _storage = storage;
        _logger = logger;
        _maximumSizeBytes = long.TryParse(configuration["SS_IMAGES_MAX_UPLOAD_BYTES"], out var configuredMaximum)
            && configuredMaximum > 0
                ? configuredMaximum
                : DefaultMaximumSizeBytes;
        var configuredLifetime = int.TryParse(configuration["SS_IMAGES_PRESIGNED_URL_TTL_SECONDS"], out var configuredSeconds)
            ? configuredSeconds
            : DefaultPresignedUrlLifetimeSeconds;
        _presignedUrlLifetime = TimeSpan.FromSeconds(
            Math.Clamp(configuredLifetime, 60, MaximumPresignedUrlLifetimeSeconds)
        );
    }

    public async Task<ContentImageInfo> UploadAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string fileName,
        Stream content,
        long declaredLength,
        CancellationToken cancellationToken
    )
    {
        if (!await OwnerExistsAsync(ownerType, ownerId, cancellationToken))
        {
            throw new KeyNotFoundException($"{ownerType} with id '{ownerId}' was not found.");
        }

        if (!ImageUploadValidator.IsWithinSizeLimit(declaredLength, _maximumSizeBytes))
        {
            throw new ArgumentException($"Image size must be between 1 and {_maximumSizeBytes} bytes.");
        }

        await using var buffer = new MemoryStream();
        var readBuffer = new byte[81920];
        long totalLength = 0;
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(readBuffer, cancellationToken)) > 0)
        {
            totalLength += bytesRead;
            if (!ImageUploadValidator.IsWithinSizeLimit(totalLength, _maximumSizeBytes))
            {
                throw new ArgumentException($"Image size must be between 1 and {_maximumSizeBytes} bytes.");
            }

            await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
        }

        if (totalLength != declaredLength)
        {
            throw new ArgumentException("The uploaded image length did not match the request length.");
        }

        var bytes = buffer.ToArray();
        if (!ImageUploadValidator.TryDetectContentType(bytes, out var contentType))
        {
            throw new ArgumentException("Only JPEG and PNG images are supported.");
        }

        var id = Guid.NewGuid();
        var objectKey = id.ToString("N");
        var safeFileName = NormalizeFileName(fileName);
        var image = new ContentImage(
            id,
            ownerType,
            ownerId,
            objectKey,
            safeFileName,
            contentType,
            totalLength,
            DateTime.UtcNow
        );

        await using var uploadStream = new MemoryStream(bytes, writable: false);
        await _storage.PutAsync(objectKey, uploadStream, contentType, cancellationToken);
        try
        {
            _dbContext.ContentImages.Add(image);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await _storage.DeleteAsync(objectKey, CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                _logger.LogError(cleanupException, "Failed to clean up image object {ObjectKey} after metadata write failure", objectKey);
            }

            throw;
        }

        return ToInfo(image);
    }

    public async Task<IReadOnlyList<ContentImageInfo>> GetReferencedAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string content,
        CancellationToken cancellationToken
    )
    {
        var imageIds = ContentImageReference.FindIds(content);
        if (imageIds.Count == 0)
        {
            return [];
        }

        var images = await _dbContext
            .ContentImages.Where(image =>
                imageIds.Contains(image.Id)
                && image.OwnerType == ownerType
                && image.OwnerId == ownerId
                && !image.IsDeletePending
            )
            .ToListAsync(cancellationToken);

        return images.Select(ToInfo).ToList();
    }

    public async Task<string> ResolveReferencesAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string content,
        CancellationToken cancellationToken
    )
    {
        var images = await GetReferencedAsync(ownerType, ownerId, content, cancellationToken);
        var urlsById = images.ToDictionary(image => image.Id, image => image.Url);
        return ContentImageReference.ReplaceWithUrls(content, urlsById);
    }

    public async Task<string> RestoreReferencesAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string content,
        CancellationToken cancellationToken
    )
    {
        var images = await _dbContext
            .ContentImages.Where(image =>
                image.OwnerType == ownerType && image.OwnerId == ownerId && !image.IsDeletePending
            )
            .Select(image => new { image.ObjectKey, image.Id })
            .ToListAsync(cancellationToken);
        if (images.Count == 0)
        {
            return content;
        }

        var referencesByObjectKey = images.ToDictionary(
            image => image.ObjectKey,
            image => $"content-image:{image.Id:D}"
        );

        return UrlPattern.Replace(
            content,
            match =>
            {
                if (
                    !Uri.TryCreate(match.Value, UriKind.Absolute, out var uri)
                    || !IsS3PresignedUrl(uri)
                    || !referencesByObjectKey.TryGetValue(
                        Uri.UnescapeDataString(uri.AbsolutePath.TrimEnd('/').Split('/').Last()),
                        out var reference
                    )
                )
                {
                    return match.Value;
                }

                return reference;
            }
        );
    }

    private static bool IsS3PresignedUrl(Uri uri)
    {
        var hostLabels = uri.IdnHost.Split('.');
        var isAwsHost = uri.IdnHost.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase)
            || uri.IdnHost.EndsWith(".amazonaws.com.cn", StringComparison.OrdinalIgnoreCase);
        var hasS3HostLabel = hostLabels.Any(label => label.StartsWith("s3", StringComparison.OrdinalIgnoreCase));
        return isAwsHost && hasS3HostLabel && uri.Query.Contains("X-Amz-Signature=", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> DeleteAsync(Guid imageId, CancellationToken cancellationToken)
    {
        var image = await _dbContext.ContentImages.SingleOrDefaultAsync(
            existing => existing.Id == imageId,
            cancellationToken
        );
        if (image is null)
        {
            return false;
        }

        image.MarkDeletePending();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(image.ObjectKey, cancellationToken);
        _dbContext.ContentImages.Remove(image);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task DeleteForOwnerAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        CancellationToken cancellationToken
    )
    {
        var imageIds = await _dbContext
            .ContentImages.Where(image => image.OwnerType == ownerType && image.OwnerId == ownerId)
            .Select(image => image.Id)
            .ToListAsync(cancellationToken);

        foreach (var imageId in imageIds)
        {
            await DeleteAsync(imageId, cancellationToken);
        }
    }

    private async Task<bool> OwnerExistsAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        CancellationToken cancellationToken
    ) => ownerType switch
    {
        ContentImageOwnerType.NewsItem => await _dbContext.NewsItems.AnyAsync(item => item.Id == ownerId, cancellationToken),
        ContentImageOwnerType.Event => await _dbContext.Events.AnyAsync(item => item.Id == ownerId, cancellationToken),
        ContentImageOwnerType.ReleaseNote => await _dbContext.ReleaseNotes.AnyAsync(item => item.Id == ownerId, cancellationToken),
        ContentImageOwnerType.EmailCampaign => await _dbContext.EmailCampaigns.AnyAsync(item => item.Id == ownerId, cancellationToken),
        _ => false,
    };

    private ContentImageInfo ToInfo(ContentImage image)
    {
        var now = DateTime.UtcNow;
        return new ContentImageInfo(
            image.Id,
            image.OwnerType,
            image.OwnerId,
            image.FileName,
            image.ContentType,
            image.Length,
            image.ObjectKey,
            $"content-image:{image.Id:D}",
            _storage.GetPresignedReadUrl(image.ObjectKey, _presignedUrlLifetime),
            now.Add(_presignedUrlLifetime),
            image.CreatedAt
        );
    }

    private static string NormalizeFileName(string fileName)
    {
        var normalized = fileName.Replace('\\', '/').Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "image";
        }

        return normalized.Length <= 255 ? normalized : normalized[..255];
    }
}
using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IContentImageService
{
    Task<ContentImageInfo> UploadAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string fileName,
        Stream content,
        long declaredLength,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ContentImageInfo>> GetReferencedAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string content,
        CancellationToken cancellationToken
    );

    Task<string> ResolveReferencesAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string content,
        CancellationToken cancellationToken
    );

    Task<string> RestoreReferencesAsync(
        ContentImageOwnerType ownerType,
        Guid ownerId,
        string content,
        CancellationToken cancellationToken
    );

    Task<bool> DeleteAsync(Guid imageId, CancellationToken cancellationToken);
    Task DeleteForOwnerAsync(ContentImageOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken);
}

public sealed record ContentImageInfo(
    Guid Id,
    ContentImageOwnerType OwnerType,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long Length,
    string ObjectKey,
    string Reference,
    string Url,
    DateTime UrlExpiresAt,
    DateTime CreatedAt
);
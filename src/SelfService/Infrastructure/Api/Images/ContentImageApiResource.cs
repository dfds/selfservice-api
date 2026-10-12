using SelfService.Application;

namespace SelfService.Infrastructure.Api.Images;

public sealed record ContentImageApiResource(
    Guid Id,
    string OwnerType,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long Length,
    string Reference,
    string Url,
    DateTime UrlExpiresAt,
    DateTime CreatedAt
)
{
    public static ContentImageApiResource From(ContentImageInfo image) => new(
        image.Id,
        image.OwnerType.ToString(),
        image.OwnerId,
        image.FileName,
        image.ContentType,
        image.Length,
        image.Reference,
        image.Url,
        image.UrlExpiresAt,
        image.CreatedAt
    );
}
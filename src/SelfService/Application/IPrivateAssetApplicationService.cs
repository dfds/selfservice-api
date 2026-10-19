using SelfService.Domain.Models;

namespace SelfService.Application;

public interface IPrivateAssetApplicationService
{
    Task<PrivateAsset> Upload(
        Guid ownerId,
        PrivateAssetOwnerType ownerType,
        string contentId,
        bool isInline,
        string fileName,
        string mimeType,
        long sizeInBytes,
        Stream content,
        PortalUser user,
        CancellationToken cancellationToken = default
    );
    Task<PrivateAsset> Get(PrivateAssetId id);
    Task<List<PrivateAsset>> GetByOwner(Guid ownerId, PrivateAssetOwnerType ownerType);
    Task<string?> GetPreviewUrl(PrivateAsset asset, CancellationToken cancellationToken = default);
}

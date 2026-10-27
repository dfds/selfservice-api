using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Application;

public class PrivateAssetApplicationService(
    IPrivateAssetRepository repository,
    IPrivateAssetService storage,
    IAuthorizationService authorization,
    IDbTransactionFacade transactions,
    ILogger<PrivateAssetApplicationService> logger
) : IPrivateAssetApplicationService
{
    public async Task<PrivateAsset> Upload(
        Guid ownerId,
        PrivateAssetOwnerType ownerType,
        bool isInline,
        string fileName,
        string mimeType,
        long sizeInBytes,
        Stream content,
        PortalUser user,
        CancellationToken cancellationToken = default
    )
    {
        if (!authorization.CanCreatePrivateAsset(user))
            throw new UnauthorizedAccessException("The private-assets:create permission is required.");

        var assetId = PrivateAssetId.New();
        var createdAt = DateTime.UtcNow;
        var createdBy = user.Id.ToString();
        PrivateAsset.ValidateMetadata(fileName, mimeType, sizeInBytes, createdBy, ownerId, ownerType);

        var key = await storage.UploadAsync(fileName, mimeType, content, cancellationToken);
        var asset = new PrivateAsset(
            assetId,
            key,
            fileName,
            mimeType,
            sizeInBytes,
            createdBy,
            createdAt,
            ownerId,
            ownerType,
            isInline
        );
        try
        {
            await using var transaction = await transactions.BeginTransaction();
            await repository.Add(asset);
            cancellationToken.ThrowIfCancellationRequested();
            await transaction.Commit();
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Asset metadata commit failed or is uncertain for {AssetId}, {StorageKey}, {OwnerType}/{OwnerId}; reconcile retained object",
                assetId,
                key,
                ownerType,
                ownerId
            );
            throw;
        }
        return asset;
    }

    public async Task<PrivateAsset> Get(PrivateAssetId id)
    {
        return await repository.FindById(id) ?? throw EntityNotFoundException<PrivateAsset>.UsingId(id.ToString());
    }

    public Task<List<PrivateAsset>> GetByOwner(Guid ownerId, PrivateAssetOwnerType ownerType) =>
        repository.GetByOwner(ownerType, ownerId);

    public async Task<string?> GetPreviewUrl(PrivateAsset asset, CancellationToken cancellationToken = default)
    {
        try
        {
            return await storage.GetPrivateUrlAsync(asset.StorageKey, TimeSpan.FromMinutes(15), cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Preview URL unavailable for committed asset {AssetId}", asset.Id);
            return null;
        }
    }
}

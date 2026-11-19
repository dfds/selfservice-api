namespace SelfService.Domain.Models;

public interface IPrivateAssetRepository
{
    Task Add(PrivateAsset asset);
    Task Remove(PrivateAsset asset);
    Task<PrivateAsset?> FindById(PrivateAssetId id);
    Task<List<PrivateAsset>> GetByOwner(PrivateAssetOwnerType ownerType, Guid ownerId);
    Task Claim(IEnumerable<Guid> assetIds, PrivateAssetOwnerType ownerType, Guid ownerId, string createdBy);
}

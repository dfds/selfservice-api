namespace SelfService.Domain.Models;

public interface IPrivateAssetRepository
{
    Task Add(PrivateAsset asset);
    Task<PrivateAsset?> FindById(PrivateAssetId id);
    Task<List<PrivateAsset>> GetByOwner(PrivateAssetOwnerType ownerType, Guid ownerId);
}

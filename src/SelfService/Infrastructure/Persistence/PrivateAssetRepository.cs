using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class PrivateAssetRepository(SelfServiceDbContext dbContext) : IPrivateAssetRepository
{
    public async Task Add(PrivateAsset asset) => await dbContext.PrivateAssets.AddAsync(asset);

    public Task Remove(PrivateAsset asset)
    {
        dbContext.PrivateAssets.Remove(asset);
        return Task.CompletedTask;
    }

    public async Task<PrivateAsset?> FindById(PrivateAssetId id) => await dbContext.PrivateAssets.FindAsync(id);

    public Task<List<PrivateAsset>> GetByOwner(PrivateAssetOwnerType ownerType, Guid ownerId) =>
        dbContext
            .PrivateAssets.Where(asset => asset.OwnerType == ownerType && asset.OwnerId == ownerId)
            .OrderBy(asset => asset.CreatedAt)
            .ThenBy(asset => asset.Id)
            .ToListAsync();

    public async Task Claim(IEnumerable<Guid> assetIds, PrivateAssetOwnerType ownerType, Guid ownerId, string createdBy)
    {
        foreach (var value in assetIds.Distinct().OrderBy(value => value))
        {
            var assetId = PrivateAssetId.NewFrom(value);
            var updated = await dbContext
                .PrivateAssets.Where(asset =>
                    asset.Id == assetId
                    && asset.OwnerType == ownerType
                    && (asset.OwnerId == ownerId || (asset.OwnerId == null && asset.CreatedBy == createdBy))
                )
                .ExecuteUpdateAsync(setters => setters.SetProperty(asset => asset.OwnerId, ownerId));

            if (updated != 1)
                throw new ArgumentException("One or more assets cannot be attached to this item.");
        }
    }
}

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
}

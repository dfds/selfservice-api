using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Persistence;

public class FavouriteRepository : GenericRepository<Favourite, FavouriteId>, IFavouriteRepository
{
    public FavouriteRepository(SelfServiceDbContext dbContext)
        : base(dbContext.Favourites) { }

    public async Task<bool> IsFavourite(CapabilityId capabilityId, UserId userId)
    {
        var favourites = await GetAllWithPredicate(x => x.CapabilityId == capabilityId && x.UserId == userId);
        return favourites.Count > 0;
    }

    public async Task<Favourite?> FindBy(CapabilityId capabilityId, UserId userId)
    {
        return await FindByPredicate(x => x.CapabilityId == capabilityId && x.UserId == userId);
    }

    public async Task<Favourite?> RemoveWithCapabilityId(CapabilityId capabilityId, UserId userId)
    {
        var favourite = await FindByPredicate(x => x.CapabilityId == capabilityId && x.UserId == userId);
        if (favourite == null)
        {
            return null;
        }

        await Remove(favourite.Id);

        return favourite;
    }

    public async Task<List<Favourite>> GetAllFavouritesForUserId(UserId userId)
    {
        return await GetAllWithPredicate(x => x.UserId == userId);
    }
}

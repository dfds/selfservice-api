namespace SelfService.Domain.Models;

public interface IFavouriteRepository : IGenericRepository<Favourite, FavouriteId>
{
    Task<bool> IsFavourite(CapabilityId capabilityId, UserId userId);
    Task<Favourite?> FindBy(CapabilityId capabilityId, UserId userId);
    Task<Favourite?> RemoveWithCapabilityId(CapabilityId capabilityId, UserId userId);
    Task<List<Favourite>> GetAllFavouritesForUserId(UserId userId);
}

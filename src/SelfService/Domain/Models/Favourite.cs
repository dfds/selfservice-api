namespace SelfService.Domain.Models;

public class Favourite : AggregateRoot<FavouriteId>
{
    public Favourite(FavouriteId id, CapabilityId capabilityId, UserId userId, DateTime createdAt)
        : base(id)
    {
        CapabilityId = capabilityId;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public CapabilityId CapabilityId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Favourite CreateFor(CapabilityId capabilityId, UserId userId, DateTime createdAt)
    {
        return new Favourite(
            id: FavouriteId.New(),
            capabilityId: capabilityId,
            userId: userId,
            createdAt: createdAt
        );
    }
}

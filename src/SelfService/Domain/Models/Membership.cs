using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class Membership : AggregateRoot<MembershipId>
{
    public Membership(MembershipId id, CapabilityId capabilityId, UserId userId, DateTime createdAt) : base(id)
    {
        CapabilityId = capabilityId;
        UserId = userId;
        CreatedAt = createdAt;
    }

    public CapabilityId CapabilityId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public override string ToString()
    {
        return $"Cap: {CapabilityId}, Usr: {UserId}";
    }

    public static Membership CreateFor(CapabilityId capabilityId, UserId userId, DateTime createdAt)
    {
        var instance = new Membership(
            id: MembershipId.New(),
            capabilityId: capabilityId,
            userId: userId,
            createdAt: createdAt
        );

        instance.Raise(new UserHasJoinedCapability
        {
            MembershipId = instance.Id.ToString(),
            CapabilityId = capabilityId,
            UserId = userId,
        });

        return instance;
    }
}
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacGroupMember : AggregateRoot<RbacGroupMemberId>
{
    public DateTime CreatedAt { get; private set; }
    public RbacGroupId GroupId { get; private set; }
    public string UserId { get; private set; }

    public RbacGroupMember(RbacGroupMemberId id, DateTime createdAt, RbacGroupId groupId, string userId ) : base(id)
    {
        CreatedAt = createdAt;
        GroupId = groupId;
        UserId = userId;
    }

    public static RbacGroupMember New(string name, string description, RbacGroupId groupId, string userId)
    {
        var instance = new RbacGroupMember(
            id: RbacGroupMemberId.New(),
            createdAt: DateTime.Now,
            groupId: groupId,
            userId: userId
            );

        // raise event
        instance.RaiseEvent(new RbacGroupMemberCreated());
        return instance;
    }

    private void RaiseEvent(IDomainEvent domainEvent)
    {
        Raise(domainEvent);
    }
}
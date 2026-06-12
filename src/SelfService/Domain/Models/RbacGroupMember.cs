using System.Text.Json.Serialization;
using SelfService.Domain.Events;

namespace SelfService.Domain.Models;

public class RbacGroupMember : AggregateRoot<RbacGroupMemberId>
{
    public DateTime CreatedAt { get; private set; }
    public RbacGroupId GroupId { get; private set; }
    public string UserId { get; private set; }

    public RbacGroupMember(RbacGroupMemberId id, DateTime createdAt, RbacGroupId groupId, string userId)
        : base(id)
    {
        CreatedAt = createdAt;
        GroupId = groupId;
        UserId = userId;
    }

    public static RbacGroupMember New(RbacGroupId groupId, string userId)
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

// Accepts either a regular User member or a ServicePrincipal member.
// `MemberId` is the canonical field; `UserId` is preserved as a deserialization alias
// for backwards compatibility with existing callers.
public class RbacGroupMemberCreationDTO
{
    public string GroupId { get; set; }
    public string? MemberId { get; set; }

    [JsonPropertyName("userId")]
    public string? UserIdAlias { get; set; }

    [JsonIgnore]
    public string UserId => MemberId ?? UserIdAlias ?? "";

    public RbacGroupMemberCreationDTO(string groupId, string? memberId = null, string? userIdAlias = null)
    {
        GroupId = groupId;
        MemberId = memberId;
        UserIdAlias = userIdAlias;
    }
}

using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class MembershipBuilder
{
    private MembershipId _id;
    private CapabilityId _capabilityId;
    private UserId _userId;
    private DateTime _createdAt;

    public MembershipBuilder()
    {
        _id = MembershipId.New();
        _capabilityId = CapabilityId.CreateFrom("foo");
        _userId = UserId.Parse("bar");
        _createdAt = new DateTime(2000, 1, 1);
    }

    public MembershipBuilder WithCapabilityId(CapabilityId capabilityId)
    {
        _capabilityId = capabilityId;
        return this;
    }

    public MembershipBuilder WithUserId(UserId userId)
    {
        _userId = userId;
        return this;
    }

    public Membership Build()
    {
        return new Membership(_id, _capabilityId, _userId, _createdAt);
    }

    public static implicit operator Membership(MembershipBuilder builder)
        => builder.Build();
}
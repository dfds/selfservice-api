using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class MemberBuilder
{
    private UserId _id;
    private string _email;
    private string? _displayName;

    public MemberBuilder()
    {
        _id = UserId.Parse("foo");
        _email = "foo@foo.com";
        _displayName = "bar";
    }

    public MemberBuilder WithUserId(UserId id)
    {
        _id = id;
        return this;
    }

    public MemberBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public Member Build()
    {
        return new Member(_id, _email, _displayName);
    }

    public static implicit operator Member(MemberBuilder builder) => builder.Build();
}

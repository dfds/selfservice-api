using SelfService.Domain.Models;

namespace SelfService.Tests.Builders;

public class MemberBuilder
{
    private UserId _id;
    private string _email;
    private string? _displayName;
    private UserSettings _userSettings;
    private bool _isServicePrincipal;

    public MemberBuilder()
    {
        _id = UserId.Parse("foo");
        _email = "foo@foo.com";
        _displayName = "bar";
        _userSettings = new UserSettings();
    }

    public MemberBuilder WithUserId(UserId id)
    {
        _id = id;
        return this;
    }

    public MemberBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public MemberBuilder WithDisplayName(string displayName)
    {
        _displayName = displayName;
        return this;
    }

    public MemberBuilder WithSettings(UserSettings settings)
    {
        _userSettings = settings;
        return this;
    }

    public MemberBuilder AsServicePrincipal()
    {
        _isServicePrincipal = true;
        return this;
    }

    public Member Build()
    {
        if (_isServicePrincipal)
        {
            return Member.RegisterServicePrincipal(_id, _email, _displayName);
        }

        return new Member(_id, _email, _displayName, _userSettings);
    }

    public static implicit operator Member(MemberBuilder builder) => builder.Build();
}

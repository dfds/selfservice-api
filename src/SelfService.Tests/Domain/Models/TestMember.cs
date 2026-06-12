using SelfService.Domain.Models;

namespace SelfService.Tests.Domain.Models;

public class TestMember
{
    [Fact]
    public void register_service_principal_sets_type_correctly()
    {
        var sp = Member.RegisterServicePrincipal(
            UserId.Parse("aabbccdd-eeff-0011-2233-445566778899"),
            "my-app-aabbccdd.s@dfds.cloud",
            "My App"
        );

        Assert.Equal(MemberType.ServicePrincipal, sp.Type);
        Assert.Equal("my-app-aabbccdd.s@dfds.cloud", sp.Email);
        Assert.Equal("My App", sp.DisplayName);
    }

    [Fact]
    public void register_user_keeps_user_type()
    {
        var user = Member.Register(UserId.Parse("user@example.com"), "user@example.com", "User Name", null);
        Assert.Equal(MemberType.User, user.Type);
    }

    [Fact]
    public void should_receive_email_is_false_for_service_principals()
    {
        var sp = Member.RegisterServicePrincipal(
            UserId.Parse("aabbccdd-eeff-0011-2233-445566778899"),
            "synthetic.s@dfds.cloud",
            "App"
        );

        Assert.False(sp.ShouldReceiveEmail);
    }

    [Fact]
    public void should_receive_email_is_true_for_users_with_email()
    {
        var user = Member.Register(UserId.Parse("user@example.com"), "user@example.com", "User Name", null);
        Assert.True(user.ShouldReceiveEmail);
    }

    [Fact]
    public void should_receive_email_is_false_for_users_with_empty_email()
    {
        var user = Member.Register(UserId.Parse("user@example.com"), "", "User Name", null);
        Assert.False(user.ShouldReceiveEmail);
    }

    [Fact]
    public void update_rejects_email_change_on_service_principal()
    {
        var sp = Member.RegisterServicePrincipal(
            UserId.Parse("aabbccdd-eeff-0011-2233-445566778899"),
            "synthetic.s@dfds.cloud",
            "App"
        );

        Assert.Throws<InvalidOperationException>(() => sp.Update("other@example.com", "Renamed"));
    }

    [Fact]
    public void update_service_principal_display_name_works()
    {
        var sp = Member.RegisterServicePrincipal(
            UserId.Parse("aabbccdd-eeff-0011-2233-445566778899"),
            "synthetic.s@dfds.cloud",
            "App"
        );

        sp.UpdateServicePrincipalDisplayName("New Name");
        Assert.Equal("New Name", sp.DisplayName);
    }

    [Fact]
    public void update_service_principal_display_name_rejected_on_user()
    {
        var user = Member.Register(UserId.Parse("user@example.com"), "user@example.com", "User Name", null);
        Assert.Throws<InvalidOperationException>(() => user.UpdateServicePrincipalDisplayName("Other"));
    }
}

using System.Security.Claims;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Tests.Infrastructure.Api;

public class TestClaimsPrincipleExtensions
{
    [Fact]
    public void slugify_lowercases_and_replaces_whitespace()
    {
        Assert.Equal("my-app", ClaimsPrincipleExtensions.Slugify("My App"));
        Assert.Equal("my-cool-app", ClaimsPrincipleExtensions.Slugify("  My   Cool   App  "));
    }

    [Fact]
    public void slugify_drops_non_alphanumerics()
    {
        Assert.Equal("appname", ClaimsPrincipleExtensions.Slugify("app#name!"));
        Assert.Equal("appname", ClaimsPrincipleExtensions.Slugify("app_name"));
    }

    [Fact]
    public void slugify_handles_empty_or_null()
    {
        Assert.Equal(string.Empty, ClaimsPrincipleExtensions.Slugify(null));
        Assert.Equal(string.Empty, ClaimsPrincipleExtensions.Slugify(""));
        Assert.Equal(string.Empty, ClaimsPrincipleExtensions.Slugify("   "));
    }

    [Fact]
    public void build_synthetic_email_uses_slug_and_first_8_hex()
    {
        var email = ClaimsPrincipleExtensions.BuildSyntheticEmail("AABBCCDD-eeff-0011-2233-445566778899", "My App");
        Assert.Equal("my-app-aabbccdd.s@dfds.cloud", email);
    }

    [Fact]
    public void build_synthetic_email_falls_back_to_full_oid_when_display_name_missing()
    {
        var email = ClaimsPrincipleExtensions.BuildSyntheticEmail("AABBCCDD-eeff-0011-2233-445566778899", null);
        Assert.Equal("aabbccdd-eeff-0011-2233-445566778899.s@dfds.cloud", email);
    }

    [Fact]
    public void build_synthetic_email_falls_back_to_full_oid_when_slug_is_empty()
    {
        var email = ClaimsPrincipleExtensions.BuildSyntheticEmail("AABBCCDD-eeff-0011-2233-445566778899", "###");
        Assert.Equal("aabbccdd-eeff-0011-2233-445566778899.s@dfds.cloud", email);
    }

    [Fact]
    public void try_get_caller_identity_returns_service_principal_for_app_idtyp()
    {
        var principal = BuildPrincipal(
            ("idtyp", "app"),
            ("oid", "aabbccdd-eeff-0011-2233-445566778899"),
            ("app_displayname", "Self Service Bot")
        );

        var caller = principal.TryGetCallerIdentity();
        Assert.NotNull(caller);
        Assert.Equal(MemberType.ServicePrincipal, caller!.Type);
        Assert.Equal("aabbccdd-eeff-0011-2233-445566778899", caller.Id.ToString());
        Assert.Equal("self-service-bot-aabbccdd.s@dfds.cloud", caller.Email);
        Assert.Equal("Self Service Bot", caller.DisplayName);
    }

    [Fact]
    public void try_get_caller_identity_returns_service_principal_with_full_oid_when_no_display_name()
    {
        var principal = BuildPrincipal(
            ("idtyp", "app"),
            ("oid", "aabbccdd-eeff-0011-2233-445566778899"),
            ("appid", "some-app-id")
        );

        var caller = principal.TryGetCallerIdentity();
        Assert.NotNull(caller);
        Assert.Equal(MemberType.ServicePrincipal, caller!.Type);
        Assert.Equal("aabbccdd-eeff-0011-2233-445566778899.s@dfds.cloud", caller.Email);
        Assert.Equal("some-app-id", caller.DisplayName);
    }

    [Fact]
    public void try_get_caller_identity_returns_null_for_app_token_without_oid()
    {
        var principal = BuildPrincipal(("idtyp", "app"));
        Assert.Null(principal.TryGetCallerIdentity());
    }

    [Fact]
    public void try_get_caller_identity_returns_user_for_normal_token()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim("preferred_username", "user@dfds.com"), new Claim("name", "User Name") },
            authenticationType: "Test",
            nameType: ClaimTypes.NameIdentifier,
            roleType: ClaimTypes.Role
        );
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user@dfds.com"));
        var principal = new ClaimsPrincipal(identity);

        var caller = principal.TryGetCallerIdentity();
        Assert.NotNull(caller);
        Assert.Equal(MemberType.User, caller!.Type);
        Assert.Equal("user@dfds.com", caller.Id.ToString());
        Assert.Equal("user@dfds.com", caller.Email);
        Assert.Equal("User Name", caller.DisplayName);
    }

    [Fact]
    public void try_get_user_id_returns_oid_for_service_principal_token()
    {
        var principal = BuildPrincipal(("idtyp", "app"), ("oid", "aabbccdd-eeff-0011-2233-445566778899"));

        Assert.True(principal.TryGetUserId(out var userId));
        Assert.Equal("aabbccdd-eeff-0011-2233-445566778899", userId.ToString());
    }

    [Fact]
    public void try_get_user_id_returns_false_for_app_token_without_oid()
    {
        var principal = BuildPrincipal(("idtyp", "app"));
        Assert.False(principal.TryGetUserId(out _));
    }

    [Fact]
    public void try_get_user_id_returns_identity_name_for_normal_user()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user@dfds.com") },
            authenticationType: "Test",
            nameType: ClaimTypes.NameIdentifier,
            roleType: ClaimTypes.Role
        );
        var principal = new ClaimsPrincipal(identity);

        Assert.True(principal.TryGetUserId(out var userId));
        Assert.Equal("user@dfds.com", userId.ToString());
    }

    private static ClaimsPrincipal BuildPrincipal(params (string Type, string Value)[] claims)
    {
        var identity = new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}

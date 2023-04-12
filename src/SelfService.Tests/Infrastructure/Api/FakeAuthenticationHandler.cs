using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SelfService.Tests.Infrastructure.Api;

public static class FakeAuthenticationSchemeDefaults
{
    public const string AuthenticationScheme = "Fake";
    public const string DefaultUser = "foo@bar.com";
}

public class FakeAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public string? Name { get; set; } = FakeAuthenticationSchemeDefaults.DefaultUser;
    public string? Upn { get; set; } = FakeAuthenticationSchemeDefaults.DefaultUser;
    public string? Role { get; set; }
    public IList<Claim> Claims { get; } = new List<Claim>();
}

public class FakeAuthenticationHandler : AuthenticationHandler<FakeAuthenticationSchemeOptions>
{
    private readonly FakeAuthenticationSchemeOptions _options;

    public FakeAuthenticationHandler(IOptionsMonitor<FakeAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
        _options = options.CurrentValue;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.Request.Headers.Authorization != FakeAuthenticationSchemeDefaults.AuthenticationScheme)
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown authorization scheme"));
        }

        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(_options.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, _options.Name));
        }
        if (!string.IsNullOrWhiteSpace(_options.Upn))
        {
            claims.Add(new Claim(ClaimTypes.Upn, _options.Upn));
        }
        if (!string.IsNullOrWhiteSpace(_options.Role))
        {
            claims.Add(new Claim(ClaimTypes.Role, _options.Role));
        }

        claims.AddRange(_options.Claims);

        var identity = new ClaimsIdentity(claims, FakeAuthenticationSchemeDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, FakeAuthenticationSchemeDefaults.AuthenticationScheme);
        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
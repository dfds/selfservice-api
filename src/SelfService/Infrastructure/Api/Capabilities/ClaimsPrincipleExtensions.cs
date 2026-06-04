using System.Security.Claims;
using System.Text;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public record CallerIdentity(UserId Id, MemberType Type, string Email, string? DisplayName);

public static class ClaimsPrincipleExtensions
{
    // v2.0 access tokens drop `unique_name` (which Identity.Name maps from in v1.0) in favour of
    // `preferred_username`. Fall back through the most common alternatives so tokens of either
    // version resolve to a user id.
    private static readonly string[] UserIdClaimTypes =
    {
        "preferred_username",
        "upn",
        ClaimTypes.Upn,
        ClaimTypes.Email,
        "email",
    };

    public static string? GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        var name = principal.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        foreach (var claimType in UserIdClaimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private const string ServicePrincipalEmailDomain = "dfds.cloud";
    private const string ServicePrincipalEmailMarker = ".s";

    // v2.0 access tokens drop `unique_name` (which Identity.Name maps from in v1.0) in favour of
    // `preferred_username`. Fall back through the most common alternatives so tokens of either
    // version resolve to a user id.
    private static readonly string[] UserIdClaimTypes =
    {
        "preferred_username",
        "upn",
        ClaimTypes.Upn,
        ClaimTypes.Email,
        "email",
    };

    public static string? GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        var name = principal.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        foreach (var claimType in UserIdClaimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public static bool TryGetUserId(this ClaimsPrincipal principal, out UserId userId)
    {
        if (UserId.TryParse(principal.GetUserId(), out userId))
        {
            return true;
        }

        var idtyp = principal?.FindFirst("idtyp")?.Value;
        if (string.Equals(idtyp, "app", StringComparison.OrdinalIgnoreCase))
        {
            var oid = FindFirst(principal!, "oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");
            if (UserId.TryParse(oid, out userId))
            {
                return true;
            }
        }

        userId = default!;
        return false;
    }

    public static PortalUser ToPortalUser(this ClaimsPrincipal principal)
    {
        var caller = principal.TryGetCallerIdentity();
        if (caller == null)
        {
            throw new Exception($"Unable to extract caller identity from ClaimsPrincipal {principal}");
        }

        var roles = principal
            .Identities.SelectMany(identity => identity.Claims)
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => UserRole.Parse(claim.Value));

        return new PortalUser(caller.Id, roles);
    }

    public static CallerIdentity? TryGetCallerIdentity(this ClaimsPrincipal principal)
    {
        if (principal == null)
        {
            return null;
        }

        var idtyp = principal.FindFirst("idtyp")?.Value;
        if (string.Equals(idtyp, "app", StringComparison.OrdinalIgnoreCase))
        {
            return BuildServicePrincipalIdentity(principal);
        }

        var rawUserId = principal.GetUserId();
        if (!UserId.TryParse(rawUserId, out var userId))
        {
            return null;
        }

        var email =
            FindFirst(principal, "preferred_username", "upn", ClaimTypes.Email, "email") ?? rawUserId ?? string.Empty;
        var displayName = FindFirst(principal, "name", ClaimTypes.Name);
        return new CallerIdentity(userId, MemberType.User, email, displayName);
    }

    private static CallerIdentity? BuildServicePrincipalIdentity(ClaimsPrincipal principal)
    {
        var oid = FindFirst(principal, "oid", "http://schemas.microsoft.com/identity/claims/objectidentifier");
        if (string.IsNullOrWhiteSpace(oid))
        {
            return null;
        }

        if (!UserId.TryParse(oid, out var userId))
        {
            return null;
        }

        var appDisplayName = FindFirst(principal, "app_displayname", "appname", "azp_acr", "azp");
        var appId = FindFirst(principal, "appid", "azp", "http://schemas.microsoft.com/identity/claims/appid");
        var email = BuildSyntheticEmail(oid, appDisplayName);
        var displayName = !string.IsNullOrWhiteSpace(appDisplayName) ? appDisplayName : appId;

        return new CallerIdentity(userId, MemberType.ServicePrincipal, email, displayName);
    }

    public static string BuildSyntheticEmail(string oid, string? appDisplayName)
    {
        if (string.IsNullOrWhiteSpace(oid))
        {
            throw new ArgumentException("oid is required to build a synthetic service-principal email.", nameof(oid));
        }

        var lowerOid = oid.ToLowerInvariant();
        var hexOnly = oid.Replace("-", string.Empty).ToLowerInvariant();
        var slug = Slugify(appDisplayName);

        if (string.IsNullOrEmpty(slug))
        {
            return $"{lowerOid}{ServicePrincipalEmailMarker}@{ServicePrincipalEmailDomain}";
        }

        var prefix = hexOnly.Length >= 8 ? hexOnly.Substring(0, 8) : hexOnly;
        return $"{slug}-{prefix}{ServicePrincipalEmailMarker}@{ServicePrincipalEmailDomain}";
    }

    public static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        var previousWasDash = false;
        foreach (var raw in value.Trim().ToLowerInvariant())
        {
            char c = raw;
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                sb.Append(c);
                previousWasDash = false;
            }
            else if (c == '-' || char.IsWhiteSpace(c))
            {
                if (!previousWasDash && sb.Length > 0)
                {
                    sb.Append('-');
                    previousWasDash = true;
                }
            }
        }

        var slug = sb.ToString().Trim('-');
        return slug;
    }

    private static string? FindFirst(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var t in claimTypes)
        {
            var value = principal.FindFirst(t)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

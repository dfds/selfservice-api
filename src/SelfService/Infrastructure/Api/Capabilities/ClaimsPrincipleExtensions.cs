using System.Security.Claims;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

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

    public static bool TryGetUserId(this ClaimsPrincipal principal, out UserId userId)
    {
        return UserId.TryParse(principal.GetUserId(), out userId);
    }

    public static PortalUser ToPortalUser(this ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            throw new Exception($"Unable to extract user id from ClaimsPrincipal {principal}");
        }

        var roles = principal
            .Identities.SelectMany(identity => identity.Claims)
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => UserRole.Parse(claim.Value));

        return new PortalUser(userId, roles);
    }
}

using System.Security.Claims;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public static class ClaimsPrincipleExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out UserId userId)
    {
        return UserId.TryParse(principal?.Identity?.Name, out userId);
    }

    public static PortalUser ToPortalUser(this ClaimsPrincipal principal)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            throw new Exception($"Unable to extract user id from ClaimsPrincipal {principal}");
        }

        var roles = principal.Identities
            .SelectMany(identity => identity.Claims)
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => UserRole.Parse(claim.Value));

        return new PortalUser(userId, roles);
    }
}
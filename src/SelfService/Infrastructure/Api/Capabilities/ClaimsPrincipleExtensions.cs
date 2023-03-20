using System.Security.Claims;
using SelfService.Domain.Models;

namespace SelfService.Infrastructure.Api.Capabilities;

public static class ClaimsPrincipleExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal principal, out UserId userId)
    {
        return UserId.TryParse(principal?.Identity?.Name, out userId);
    }
}
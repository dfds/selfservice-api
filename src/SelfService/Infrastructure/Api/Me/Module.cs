using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Me;

public static class Module
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", GetMe).WithTags("Account").Produces<Me>();
    }

    private static async Task<IResult> GetMe(SelfServiceDbContext context, ClaimsPrincipal user, LinkGenerator linkGenerator, HttpContext httpContext)
    {
        var upn = user.Identity?.Name?.ToLower();
        
        var capabilities = await context
            .Memberships
            .Include(x => x.Capability)
            .Where(x => x.UPN.ToLower() == upn)
            .OrderBy(x => x.Capability.Name)
            .Select(x => x.Capability)
            .ToListAsync();

        return TypedResults.Ok(new Me
        {
            Capabilities = capabilities
                .Select(x => new MyCapability
                {
                    Id = x.Id.ToString(),
                    RootId = x.Id.ToString(),
                    Name = x.Name,
                    Description = x.Description,
                    Links = new Link[]
                    {
                        new Link("self", linkGenerator.GetUriByName(httpContext, "capability", new { id = x.Id }))
                    }
                })
                .ToArray()
        });
    }
}
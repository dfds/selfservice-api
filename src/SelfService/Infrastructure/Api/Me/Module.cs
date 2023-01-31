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

    private static async Task<IResult> GetMe(LegacyDbContext dbContext, ClaimsPrincipal user, LinkGenerator linkGenerator, HttpContext httpContext)
    {
        var name = user.Identity.Name;

        var capabilities = await dbContext
            .Capabilities
            .Where(x => x.Memberships.Any(y => y.Email.ToLower() == name.ToLower()))
            .Where(c => c.Deleted == null)
            .Include(x => x.Memberships)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return TypedResults.Ok(new Me
        {
            Capabilities = capabilities
                .Select(x => new MyCapability
                {
                    Id = x.Id.ToString(),
                    RootId = x.RootId,
                    Name = x.Name,
                    Description = x.Description,
                    Members = x.Memberships.Select(x => x.Email).ToArray(),
                    Links = new Link[]
                    {
                        new Link("self", linkGenerator.GetUriByName(httpContext, "capability", new { id = x.Id }))
                    }
                })
                .ToArray()
        });
    }
}
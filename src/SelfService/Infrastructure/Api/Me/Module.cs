using System.Security.Claims;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Me;

public static class Module
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", GetMe).WithTags("Account").Produces<Me>();
    }

    private static async Task<IResult> GetMe(ClaimsPrincipal user, LinkGenerator linkGenerator, HttpContext httpContext, IMyCapabilitiesQuery myCapabilitiesQuery)
    {
        var errors = new Dictionary<string, string[]>();

        var upn = user.Identity?.Name?.ToLower();
        if (!UserId.TryParse(upn, out var userId))
        {
            errors.Add("upn", new[] { $"Identity \"{upn}\" is not a valid user id." });
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        var capabilities = await myCapabilitiesQuery.FindBy(userId);

        return TypedResults.Ok(new Me
        {
            Capabilities = capabilities
                .Select(x => new MyCapability
                {
                    Id = x.Id.ToString(),
                    RootId = x.Id.ToString(),
                    Name = x.Name,
                    Description = x.Description,
                    Links = new Link[] // TODO [jandr@2023-02-22]: change link field from array to object representation
                    {
                        new Link("self", linkGenerator.GetUriByName(httpContext, "capability", new { id = x.Id }))
                    }
                })
                .ToArray()
        });
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Models;
using SelfService.Domain.Queries;

namespace SelfService.Infrastructure.Api.Me;

public static class Module
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/me", GetMe).WithTags("Account");
    }

    private static async Task<IResult> GetMe(ClaimsPrincipal user, LinkGenerator linkGenerator, HttpContext httpContext, [FromServices] IMyCapabilitiesQuery myCapabilitiesQuery)
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

        return TypedResults.Ok(new
        {
            Capabilities = capabilities.Select(x => new
                {
                    Id = x.Id.ToString(),
                    Name = x.Name,
                    Description = x.Description,
                    //Links = new Link[] // TODO [jandr@2023-02-22]: change link field from array to object representation
                    //{
                    //    new Link("self", linkGenerator.GetUriByName(httpContext, "capability", new {id = x.Id}))
                    //}
                })
                .ToArray()
        });

    }
}
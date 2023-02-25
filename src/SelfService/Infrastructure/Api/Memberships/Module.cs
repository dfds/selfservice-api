using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Memberships;

public static class Module
{
    public static void MapMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/memberships").WithTags("Memberships");
        group.MapGet("{id:required}", GetById).WithName("get membership");
    }

    private static async Task<IResult> GetById(string id, ClaimsPrincipal user, SelfServiceDbContext dbContext)
    {
        var errors = new Dictionary<string, string[]>();

        if (!MembershipId.TryParse(id, out var membershipId))
        {
            errors.Add(nameof(id), new[] { "Value is not a valid membership id." });
        }

        if (!UserId.TryParse(user?.Identity?.Name, out var userId))
        {
            errors.Add("upn", new[] { "Value is not a valid user id." });
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        var found = await dbContext.Memberships.SingleOrDefaultAsync(x => x.Id == membershipId);
        if (found is null)
        {
            return Results.NotFound();
        }

        // NOTE [jandr@2023-02-23]: does this make sense at all or should it just be allowed?
        if (found.UserId != userId)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new
        {
            Id = found.Id.ToString(),
            CapabilityId = found.CapabilityId.ToString(),
            UserId = found.UserId.ToString(),
            CreatedAt = found.CreatedAt.ToString("O"),
            // TODO [jandr@2023-02-22]: add links section
        });
    }
}
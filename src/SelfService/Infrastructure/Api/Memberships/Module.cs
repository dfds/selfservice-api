using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Memberships;

public static class Module
{
    public static void MapMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/memberships").WithTags("Memberships");

        group.MapPost("", NewMembership).WithName("new membership");
        group.MapPost("{id:required}", GetById).WithName("get membership");

    }

    private static async Task<IResult> GetById(HttpContext context, string id, SelfServiceDbContext dbContext)
    {
        if (!MembershipId.TryParse(id, out var membershipId))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                {nameof(id), new[] {"Value is not a valid membership id."}}
            });
        }

        var found = await dbContext.Memberships.SingleOrDefaultAsync(x => x.Id == membershipId);
        if (found is null)
        {
            return Results.NotFound();
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

    private static async Task<IResult> NewMembership(HttpContext context, [FromBody] NewMembershipRequest newMembershipRequest, 
        IMembershipApplicationService membershipApplicationService)
    {
        var errors = new Dictionary<string, string[]>();

        if (!CapabilityId.TryParse(newMembershipRequest.CapabilityId, out var capabilityId))
        {
            errors.Add(nameof(newMembershipRequest.CapabilityId), new []{"Invalid capability id."});
        }

        if (!UserId.TryParse(newMembershipRequest.UserId, out var userId))
        {
            errors.Add(nameof(newMembershipRequest.CapabilityId), new []{"Invalid user id."});
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        var membershipId = await membershipApplicationService.StartNewMembership(capabilityId, userId);

        return Results.CreatedAtRoute("get membership", new {id = membershipId.ToString()});
    }
}

public class NewMembershipRequest
{
    [Required(AllowEmptyStrings = false)]
    public string? CapabilityId { get; set; }

    [Required(AllowEmptyStrings = false)]
    public string? UserId { get; set; }
}

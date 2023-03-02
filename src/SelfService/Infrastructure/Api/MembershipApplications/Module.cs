using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.MembershipApplications;

public static class Module
{
    public static void MapMembershipApplicationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/membershipsapplications").WithTags("Membership Applications");

        group.MapGet("{id:required}", GetById).WithName("get membership application");
        group.MapPost("", SubmitMembershipApplication).WithName("submit membership application");
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

    private static async Task<IResult> SubmitMembershipApplication([FromBody] NewMembershipApplicationRequest newMembershipApplication, 
        ClaimsPrincipal user, [FromServices] IMembershipApplicationService membershipApplicationService)
    {
        var errors = new Dictionary<string, string[]>();

        if (!CapabilityId.TryParse(newMembershipApplication.CapabilityId, out var capabilityId))
        {
            errors.Add(nameof(newMembershipApplication.CapabilityId), new []{"Invalid capability id."});
        }

        if (!UserId.TryParse(user?.Identity?.Name, out var userId))
        {
            errors.Add("upn", new []{"Invalid user id."});
        }

        if (errors.Any())
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var applicationId = await membershipApplicationService.SubmitMembershipApplication(capabilityId, userId);
            return Results.CreatedAtRoute("get membership application", new {id = applicationId.ToString()});
        }
        catch (EntityNotFoundException<Capability>)
        {
            return Results.NotFound(new ProblemDetails
            {
                Title = "Capability not found",
                Detail = $"Capability \"{capabilityId}\" is unknown by the system." 
            });
        }
        catch (PendingMembershipApplicationAlreadyExistsException)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Already has pending membership application",
                Detail = $"User \"{userId}\" already has a pending membership application for capability \"{capabilityId}\"."
            });
        }
    }
}

public class NewMembershipApplicationRequest
{
    public string? CapabilityId { get; set; }
}
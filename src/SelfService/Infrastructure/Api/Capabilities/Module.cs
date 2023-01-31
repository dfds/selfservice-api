using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Capabilities;

public static class Module
{
    public static void MapCapabilityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/capabilities").WithTags("Capability");

        group.MapGet("", GetCapabilityList);
        group.MapGet("{id:guid}", GetCapability).WithName("capability");
        group.MapPost("", NotImplemented);
        group.MapPut("{id:guid}", NotImplemented);
        group.MapDelete("{id:guid}", NotImplemented);
        group.MapPost("{id:guid}/members", NotImplemented);
        group.MapDelete("{id:guid}/members/{memberEmail}", NotImplemented);
        group.MapPost("{id:guid}/contexts", NotImplemented);
    }

    private static async Task<IResult> GetCapabilityList(LegacyDbContext dbContext)
    {
        var capabilities = await dbContext
            .Capabilities
            .Where(c => c.Deleted == null)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Results.Ok(new { items = capabilities.Select(CapabilityListItemDto.Create).ToArray() });
    }

    private static async Task<IResult> GetCapability(Guid id, LegacyDbContext dbContext)
    {
        var capability = await dbContext
            .Capabilities
            .Include(x => x.Memberships)
            .Include(x => x.Contexts)
            .SingleOrDefaultAsync(x => x.Id == id);

        if (capability == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(CapabilityDetailDto.Create(capability));
    }

    private static IResult NotImplemented()
    {
        return Results.StatusCode(StatusCodes.Status501NotImplemented);
    }
}
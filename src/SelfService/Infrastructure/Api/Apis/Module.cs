using Microsoft.EntityFrameworkCore;
using SelfService.Infrastructure.Persistence;

namespace SelfService.Infrastructure.Api.Apis;

public static class Module
{
    public static void MapServiceCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/apispecs").WithTags("ServiceCatalog");

        group.MapGet("", GetServiceCatalog).AllowAnonymous();
        group.MapGet("{id:guid}", GetService).AllowAnonymous();
    }

    private static async Task<IResult> GetServiceCatalog(SelfServiceDbContext dbContext)
    {
        var serviceDescriptions = await dbContext.ServiceCatalog.OrderBy(x => x.Name).ToListAsync();

        return Results.Ok(
            new { items = serviceDescriptions.Select(x => new ServiceDescriptionDto(x.Id, x.Name)).ToArray() }
        );
    }

    private static async Task<IResult> GetService(Guid id, SelfServiceDbContext dbContext)
    {
        var serviceDescription = await dbContext.ServiceCatalog.FirstOrDefaultAsync();
        if (serviceDescription == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(serviceDescription);
    }
}

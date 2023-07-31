using SelfService.Infrastructure.Api.Apis;

namespace SelfService.Infrastructure.Api;

public static class ModuleConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapServiceCatalogEndpoints();
    }
}

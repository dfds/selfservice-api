using SelfService.Infrastructure.Api.Apis;
using SelfService.Infrastructure.Api.Me;
using SelfService.Infrastructure.Api.System;

namespace SelfService.Infrastructure.Api;

public static class ModuleConfiguration
{
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapSystemModule();
        app.MapMeEndpoints();
        app.MapServiceCatalogEndpoints();
    }
}
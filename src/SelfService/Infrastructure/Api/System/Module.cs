using SelfService.Infrastructure.Api.Configuration;

namespace SelfService.Infrastructure.Api.System;

public static class Module
{
    public static void MapSystemModule(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => "Hello World!").WithTags("System").NoSwaggerDocs();
    }
}
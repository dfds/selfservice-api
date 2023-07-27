using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace SelfService.Infrastructure.Api.Configuration;

public static class SwaggerExtensions
{
    public static RouteHandlerBuilder NoSwaggerDocs(this RouteHandlerBuilder routeHandlerBuilder)
    {
        return routeHandlerBuilder.WithMetadata(new IgnoreSwaggerDocs());
    }

    public static bool ShouldIgnore(this ApiDescription description)
    {
        return description.ActionDescriptor.EndpointMetadata.OfType<IgnoreSwaggerDocs>().Any();
    }

    private class IgnoreSwaggerDocs
    {
    }
}
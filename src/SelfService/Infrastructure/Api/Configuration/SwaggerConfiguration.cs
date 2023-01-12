using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;

namespace SelfService.Infrastructure.Api.Configuration;

public static class SwaggerConfiguration
{
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Description = "SelfService API",
                Version = "v1",
                Title = "SelfService API"
            });

            options.DocInclusionPredicate((_, description) => !description.ShouldIgnore());
        });
    }
}

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
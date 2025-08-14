using Microsoft.OpenApi.Models;

namespace SelfService.Infrastructure.Api.Configuration;

public static class SwaggerConfiguration
{
    public static void AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Description = "SelfService API",
                    Version = "v1",
                    Title = "SelfService API",
                }
            );
            options.CustomSchemaIds(x => x.ToString());

            options.DocInclusionPredicate((_, description) => !description.ShouldIgnore());
        });
    }
}

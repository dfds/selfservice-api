using System.Text.Json;
using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api;

namespace SelfService.Configuration;

public static class Api
{
    public static void AddApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(x =>
            {
                x.AllowAnyOrigin();
                x.AllowAnyHeader();
                x.AllowAnyMethod();
                //x.WithExposedHeaders("Strict-Transport-Security");
                x.SetIsOriginAllowedToAllowWildcardSubdomains();
                x.SetIsOriginAllowed(_ => true);
            });
        });

        builder
            .Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });

        builder.Services.AddTransient<ApiResourceFactory>();
    }
}

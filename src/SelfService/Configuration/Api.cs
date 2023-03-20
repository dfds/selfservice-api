using SelfService.Infrastructure.Api.Kafka;
using System.Text.Json;
using System.Text.Json.Serialization;
using SelfService.Infrastructure.Api;

namespace SelfService.Configuration;

public static class Api
{
    public static void AddApi(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddControllers()
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
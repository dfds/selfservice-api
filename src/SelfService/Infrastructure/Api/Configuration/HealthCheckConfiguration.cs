using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SelfService.Infrastructure.Api.Configuration;

public static class HealthCheckConfiguration
{
    public static void AddHealthCheck(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy());
    }

}
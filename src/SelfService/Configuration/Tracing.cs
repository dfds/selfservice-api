using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SelfService.Configuration;

public static class DependencyInjection
{
    public static void AddTracing(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(conf =>
            {
                conf.AddService(serviceName: Tracing.ServiceName, serviceNamespace: "ssu");
            })
            .WithTracing(conf =>
            {
                conf.AddSource(Tracing.ServiceName);
                conf.AddHttpClientInstrumentation();
                conf.AddAspNetCoreInstrumentation();
                conf.AddEntityFrameworkCoreInstrumentation();

                conf.AddOtlpExporter();
            });
    }
}

public static class Tracing
{
    public const string ServiceName = "selfserviceapi";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}

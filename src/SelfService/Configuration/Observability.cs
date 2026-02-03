using System.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SelfService.Configuration;

public static class DependencyInjection
{
    public static void AddObservability(this WebApplicationBuilder builder)
    {
        if (builder.Configuration["Observability:Enabled"] == "false")
        {
            return;
        }

        builder
            .Services.AddOpenTelemetry()
            .ConfigureResource(conf =>
            {
                conf.AddService(serviceName: ObservabilityConsts.ServiceName, serviceNamespace: "ssu");
                conf.AddAttributes(new Dictionary<string, object>
                {
                    ["app"] = ObservabilityConsts.ServiceName
                });
            })
            .WithTracing(conf =>
            {
                conf.AddSource(ObservabilityConsts.ServiceName);
                conf.AddHttpClientInstrumentation();
                conf.AddAspNetCoreInstrumentation();
                conf.AddEntityFrameworkCoreInstrumentation();

                conf.AddOtlpExporter();
            })
            .WithMetrics(conf =>
            {
                conf.AddMeter(ObservabilityConsts.ServiceName);
                conf.AddAspNetCoreInstrumentation();
                conf.AddHttpClientInstrumentation();
                conf.AddRuntimeInstrumentation();
                conf.AddMeter("Npgsql");
                
                conf.AddOtlpExporter();
                conf.AddPrometheusHttpListener(conf =>
                {
                    conf.UriPrefixes = new string[] { "http://localhost:8888/" };
                });
            });
    }
}

public static class ObservabilityConsts
{
    public const string ServiceName = "selfserviceapi";
    public static readonly ActivitySource ActivitySource = new(ServiceName);
}

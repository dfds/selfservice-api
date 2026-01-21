using Grafana.OpenTelemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace SelfService.Configuration;

public static class DependencyInjection
{
    public static void AddTracing(this WebApplicationBuilder builder)
    {
        var headers = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS");
        Console.WriteLine(headers);
        builder.Services.AddOpenTelemetry()
            // .UseOtlpExporter()
            .WithTracing(conf =>
            {
                conf.UseGrafana();
                conf.AddHttpClientInstrumentation();
                conf.AddAspNetCoreInstrumentation();
            });
    }
}
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace FakeConfluentGateway.App.Configuration;

public static class Logging
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            (context, configuration) =>
            {
                configuration
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.IdentityModel", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override(
                        "Microsoft.Extensions.Http.DefaultHttpClientFactory",
                        LogEventLevel.Information
                    )
                    .MinimumLevel.Override("Microsoft.Extensions.Diagnostics.HealthChecks", LogEventLevel.Warning)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .MinimumLevel.Override("Dafda", LogEventLevel.Information);

                configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code);
            }
        );
    }
}

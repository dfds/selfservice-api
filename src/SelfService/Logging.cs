using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace SelfService;

public static class Serilog
{
    public static void AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog(
            (context, configuration) =>
            {
                _ = bool.TryParse(Environment.GetEnvironmentVariable("HUMAN_READABLE_LOG"), out var humanReadableLog);

                configuration.Enrich
                    .FromLogContext()
                    .Enrich.WithProperty("Application", "SelfService.Api")
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.IdentityModel", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                    .MinimumLevel.Override(
                        "Microsoft.Extensions.Http.DefaultHttpClientFactory",
                        LogEventLevel.Information
                    )
                    .MinimumLevel.Override("Microsoft.Extensions.Diagnostics.HealthChecks", LogEventLevel.Warning)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .MinimumLevel.Override("Dafda", LogEventLevel.Information);

                if (humanReadableLog)
                {
                    configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code);
                }
                else
                {
                    configuration.WriteTo.Console(new CompactJsonFormatter());
                }
            }
        );
    }
}

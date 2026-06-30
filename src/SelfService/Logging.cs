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

                configuration
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "SelfService.Api")
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.IdentityModel", LogEventLevel.Warning)
                    // MSAL routes its (Info-level) logging through Microsoft.Identity.Web's
                    // ITokenAcquisition logger — the "MSAL 4.x … .NET … Darwin …" banner spam
                    // on every token acquisition (e.g. the catalog token provider). Keep warnings.
                    .MinimumLevel.Override("Microsoft.Identity.Web", LogEventLevel.Warning)
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

using SelfService;
using SelfService.Configuration;
using SelfService.Infrastructure.Api;
using SelfService.Infrastructure.Api.Configuration;
using SelfService.Infrastructure.Messaging;
using SelfService.Infrastructure.Metrics;
using SelfService.Infrastructure.Persistence;
using SelfService.Legacy;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddLogging();
    builder.AddHealthCheck();
    builder.AddMetrics();
    builder.AddSwagger();
    builder.AddLegacy();
    builder.AddDatabase();
    builder.AddMessaging();
    builder.AddDomain();
    builder.AddApi();
    builder.AddSecurity();

    // **PLEASE NOTE** : keep this as the last configuration!
    builder.ConfigureAspects();

    var app = builder.Build();

    app.UseForwardedPrefixAsBasePath();
    app.UseHealthChecks("/healthz");
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapEndpoints();

    app.UseSerilogRequestLogging();
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.CloseAndFlush();
}

#pragma warning disable CA1050 // Declare types in namespaces
public partial class Program
{
}
#pragma warning disable CA1050 // Declare types in namespaces
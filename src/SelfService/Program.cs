using SelfService;
using SelfService.Domain;
using SelfService.Infrastructure.Api.Configuration;
using SelfService.Infrastructure.Metrics;
using SelfService.Infrastructure.Persistence;
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
    builder.AddDatabase();

    var app = builder.Build();

    app.UseForwardedPrefixAsBasePath();
    app.UseHealthChecks("/healthz");
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapEndpoints();
    
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
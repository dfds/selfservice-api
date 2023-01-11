using SelfService;
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

    app.MapGet("/", () => "Hello World!").WithTags("System");

    app.MapGet("/capabilities", Projections.GetCapabilityList).WithTags("Capability");
    app.MapGet("/capabilities/{id:guid}", Projections.GetCapability).WithTags("Capability");
    app.MapPost("/capabilities", Projections.NotImplemented).WithTags("Capability");
    app.MapPut("/capabilities/{id:guid}", Projections.NotImplemented).WithTags("Capability");
    app.MapDelete("/capabilities/{id:guid}", Projections.NotImplemented).WithTags("Capability");
    app.MapPost("/capabilities/{id:guid}/members", Projections.NotImplemented).WithTags("Capability");
    app.MapDelete("/capabilities/{id:guid}/members/{memberEmail}", Projections.NotImplemented).WithTags("Capability");
    app.MapPost("/capabilities/{id:guid}/contexts", Projections.NotImplemented).WithTags("Capability");

    app.MapGet("/kafka/cluster", Projections.GetClusterList).WithTags("Cluster");

    app.MapGet("/capabilities/{id:guid}/topics", Projections.GetAllByCapability).WithTags("Kafka");
    app.MapGet("/capabilities/{id:guid}/request-credential-generation", Projections.NotImplemented).WithTags("Kafka");
    app.MapGet("/topics", Projections.GetAllTopics).WithTags("Kafka");
    app.MapPost("/capabilities/{id:guid}/topics", Projections.NotImplemented).WithTags("Kafka");
    app.MapDelete("/topics/{name}", Projections.NotImplemented).WithTags("Kafka");

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
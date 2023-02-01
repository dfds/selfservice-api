using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using SelfService;
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

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthorization(opt =>
    {
        opt.FallbackPolicy = new AuthorizationPolicyBuilder()
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
    });
    
    // NOTE: enable to debug authentication issues
    // IdentityModelEventSource.ShowPII = true;
    
    var app = builder.Build();

    app.UseForwardedPrefixAsBasePath();
    app.UseHealthChecks("/healthz");
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();
    
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
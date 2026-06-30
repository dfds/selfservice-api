using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using SelfService.Infrastructure.Api;

namespace SelfService.Configuration;

public static class Security
{
    public static void AddSecurity(this WebApplicationBuilder builder)
    {
        builder
            .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
            // Enables ITokenAcquisition so selfservice-api can acquire a client-credentials
            // (app-only) token as its own service principal to call ssu-catalog downstream.
            // In local dev SS_CATALOG_SCOPE is empty, so no token is acquired (see CatalogTokenProvider).
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddInMemoryTokenCaches();

        builder.Services.AddAuthorization();

        // NOTE: enable to debug authentication issues
        // IdentityModelEventSource.ShowPII = true;

        AutoRegisterAuthorizationHandlers(builder);
    }

    private static void AutoRegisterAuthorizationHandlers(WebApplicationBuilder builder)
    {
        var types = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => typeof(IAuthorizationHandler).IsAssignableFrom(x))
            .Where(x => x is { IsClass: true, IsAbstract: false })
            .ToArray();

        foreach (var implementationType in types)
        {
            builder.Services.AddTransient(typeof(IAuthorizationHandler), implementationType);
        }
    }
}

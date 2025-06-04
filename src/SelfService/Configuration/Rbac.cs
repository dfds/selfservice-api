using SelfService.Application;
using SelfService.Infrastructure.Api.RBAC;

namespace SelfService.Configuration;

public static class Rbac
{
    public static void AddRbac(this WebApplicationBuilder builder)
    { 
        // For testing purposes, TODO: replace with implementation that uses DB & repository
        builder.Services.AddTransient<IRbacApplicationService, RbacApplicationService>(_ => RbacApplicationService.BootstrapTestService());
        builder.Services.AddTransient<AuthChecker>();
    }
}
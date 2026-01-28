using SelfService.Application;
using SelfService.Infrastructure.Api.RBAC;

namespace SelfService.Configuration;

public static class Rbac
{
    public static void AddRbac(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IRbacApplicationService, RbacApplicationService>();
        builder.Services.AddTransient<AuthChecker>();
    }
}

using Microsoft.AspNetCore.Http.Features;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.RBAC;

public class AuthChecker : IMiddleware
{
    private readonly IRbacApplicationService _rbacApplicationService;

    public AuthChecker(IRbacApplicationService rbacApplicationService)
    {
        _rbacApplicationService = rbacApplicationService;
    }
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // add logic
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var requiresPermissionAttribute = endpoint?.Metadata.GetMetadata<RequiresPermissionAttribute>();
        
        if (requiresPermissionAttribute == null) // todo: Handle when attribute is not available
        {
            await next(context);
            return;
        }
        
        PortalUser portalUser;
        try
        {
            portalUser = context.User.ToPortalUser(); // TODO: Clean up to support tokens from service accounts
        }
        catch (Exception)
        {
            // await next(context);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("");
            return;
        }

        if (!_rbacApplicationService.IsUserPermitted(portalUser.Id.ToString(),
                new List<Permission>
                    { new() { Namespace = requiresPermissionAttribute!.Ns, Name = requiresPermissionAttribute!.Name } },
                "sandbox-emcla-pmyxn").Permitted())
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync($"Missing permission {requiresPermissionAttribute!.Name}");
            return;
        }
        
        await next(context);
    }
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseAuthCheckerMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthChecker>();
    }
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class RequiresPermissionAttribute : Attribute
{
    public String Ns { get; set; }
    public String Name { get; set; }

    public RequiresPermissionAttribute(string ns, string name)
    {
        Ns = ns;
        Name = name;
    }
}

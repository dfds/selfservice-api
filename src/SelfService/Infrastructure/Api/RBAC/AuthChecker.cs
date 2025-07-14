using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
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
        var controllerRbacConfigAttribute = endpoint?.Metadata.GetMetadata<RbacConfigAttribute>();
        var controllerMetadata = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        
        if (requiresPermissionAttribute == null) // todo: Handle when attribute is not available
        {
            await next(context);
            return;
        }
        
        if (controllerRbacConfigAttribute == null)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Missing required permission attribute");
            return;
        }

        var objectKey = context.GetRouteValue(controllerRbacConfigAttribute.ObjectKey)?.ToString();
        
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

        switch (controllerRbacConfigAttribute.ObjectType)
        {
            case nameof(RbacObjectType.Capability):
                if (!(await _rbacApplicationService.IsUserPermitted(portalUser.Id.ToString(),
                        new List<Permission>
                            { new() { Namespace = requiresPermissionAttribute!.Ns, Name = requiresPermissionAttribute!.Name, AccessType = AccessType.Capability} },
                        objectKey!)).Permitted())
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync($"Missing permission {requiresPermissionAttribute!.Name}");
                    return;
                }
                break;
            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("Incorrectly configured type lookup");
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

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public class RbacConfigAttribute : Attribute
{
    public String ObjectType { get; set; }
    public String ObjectKey { get; set; }

    public RbacConfigAttribute(string objectType, string objectKey)
    {
        ObjectType = objectType;
        ObjectKey = objectKey;
    }
}

public enum RbacObjectType
{
    Capability
}

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
        var requiresPermissionAttributes = endpoint?.Metadata.GetOrderedMetadata<RequiresPermissionAttribute>();
        var controllerRbacConfigAttribute = endpoint?.Metadata.GetMetadata<RbacConfigAttribute>();

        if (requiresPermissionAttributes == null) // todo: Handle when attribute is not available
        {
            await next(context);
            return;
        }

        if (requiresPermissionAttributes.Count == 0)
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

        foreach (var requiredPermission in requiresPermissionAttributes)
        {
            switch (controllerRbacConfigAttribute.ObjectType)
            {
                case nameof(RbacObjectType.Capability):
                    if (
                        !(
                            await _rbacApplicationService.IsUserPermitted(
                                portalUser.Id.ToString(),
                                new List<Permission>
                                {
                                    new()
                                    {
                                        Namespace = requiredPermission!.Ns,
                                        Name = requiredPermission!.Name,
                                        AccessType = AccessType.Capability,
                                    },
                                },
                                objectKey!
                            )
                        ).Permitted()
                    )
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync($"Missing permission {requiredPermission!.Name}");
                        return;
                    }
                    break;
                case nameof(RbacObjectType.Global):
                    if (
                        !(
                            await _rbacApplicationService.IsUserPermitted(
                                portalUser.Id.ToString(),
                                new List<Permission>
                                {
                                    new()
                                    {
                                        Namespace = requiredPermission!.Ns,
                                        Name = requiredPermission!.Name,
                                        AccessType = AccessType.Global,
                                    },
                                },
                                objectKey!
                            )
                        ).Permitted()
                    )
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsync($"Missing permission {requiredPermission!.Name}");
                        return;
                    }
                    break;
                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Incorrectly configured type lookup");
                    return;
            }
        }

        await next(context);
    }
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseAuthCheckerMiddleware(this IApplicationBuilder app)
    {
        var conf = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var disabled = conf["SS_MIDDLEWARE_AUTHCHECKER_DISABLE"];
        if (disabled == null)
        {
            return app.UseMiddleware<AuthChecker>();
        }
        ;
        return disabled != "true" ? app.UseMiddleware<AuthChecker>() : app;
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
    Capability,
    Global,
}

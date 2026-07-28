using Microsoft.Extensions.Primitives;

namespace SelfService.Infrastructure.Api;

public class ReducedPermissionsMiddleware : IMiddleware
{
    public const string ReducedPermissionsContextKey = "reducedPermissionsRequested";

    public ReducedPermissionsMiddleware() { }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Request.Headers.TryGetValue("x-selfservice-permissions", out StringValues permissions);

        if (permissions.ToString().Equals("1"))
        {
            context.Items[ReducedPermissionsContextKey] = true;
        }

        await next(context);
    }
}
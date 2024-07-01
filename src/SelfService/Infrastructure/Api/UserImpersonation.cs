using Microsoft.Extensions.Primitives;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api;

public class UserImpersonation : IMiddleware
{
    public UserImpersonation() { }

    private enum UserPermissions
    {
        CloudEngineer,
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Request.Headers.TryGetValue("x-selfservice-permissions", out StringValues permissions);

        if (permissions.ToString().Equals("1"))
        {
            if (context.User.ToPortalUser().Roles.Count(x => x.ToString().Equals("CloudEngineer")) == 1)
            {
                context.Items["userPermissions"] = UserPermissions.CloudEngineer;
            }
        }

        await next(context);
    }
}

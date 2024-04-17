using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Messaging;

namespace SelfService.Infrastructure.Api;

public class UserActionMiddleware : IMiddleware
{
    private readonly IMessagingService _messagingService;

    public UserActionMiddleware(IMessagingService messagingService)
    {
        _messagingService = messagingService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path;
        PortalUser portalUser;
        try
        {
            portalUser = context.User.ToPortalUser(); // TODO: Clean up to support tokens from service accounts
        }
        catch (Exception)
        {
            await next(context);
            return;
        }
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;
        var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var userActionNoAuditAttribute = endpoint?.Metadata.GetMetadata<UserActionNoAuditAttribute>();
        var userActionSkipRequestDataAttribute = endpoint?.Metadata.GetMetadata<UserActionSkipRequestDataAttribute>();
        var rawRequestContent = "";

        if (userActionSkipRequestDataAttribute == null)
        {
            context.Request.EnableBuffering();
            var body = context.Request.Body;
            using (
                var reader = new StreamReader(
                    body,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    leaveOpen: true
                )
            )
            {
                rawRequestContent = await reader.ReadToEndAsync();
                body.Seek(0, SeekOrigin.Begin);
            }
        }

        if (userActionNoAuditAttribute != null) // Skip if UserActionNoAuditAttribute is set
        {
            await next(context);
            return;
        }

        var evtAction = "";
        if (action != null)
        {
            evtAction = $"{action.ControllerName}.{action.ActionName}";
        }

        DateTimeOffset currentDateTimeOffset = DateTimeOffset.Now;
        long unixTimestamp = currentDateTimeOffset.ToUnixTimeSeconds();
        var evt = new SelfService.Domain.Events.UserAction()
        {
            Action = evtAction,
            Path = path,
            Method = context.Request.Method,
            Service = "selfservice-api",
            Username = portalUser.Id,
            RequestData = rawRequestContent,
            Timestamp = unixTimestamp
        };

        await _messagingService.SendDomainEvent(evt);
        await next(context);
    }
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseUserActionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UserActionMiddleware>();
    }
}

public class UserActionNoAuditAttribute : Attribute
{
    public UserActionNoAuditAttribute() { }
}

public class UserActionSkipRequestDataAttribute : Attribute
{
    public UserActionSkipRequestDataAttribute() { }
}

namespace SelfService.Infrastructure.Api.Configuration;

public static class ForwardedPrefixConfiguration
{
    public static IApplicationBuilder UseForwardedPrefixAsBasePath(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ForwardedPrefixMiddleware>();
    }

    public class ForwardedPrefixMiddleware
    {
        private readonly RequestDelegate _next;

        public ForwardedPrefixMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var forwardedPrefix))
            {
                context.Request.PathBase = new PathString(forwardedPrefix!);
            }

            if (context.Request.Headers.TryGetValue("X-Forwarded-Proto", out var forwardedProto))
            {
                context.Request.Scheme = forwardedProto!;
            }

            await _next(context);
        }
    }
}
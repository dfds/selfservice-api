using System.Security.Claims;

namespace SelfService.Configuration;

public static class Impersonation
{
    public static void UseImpersonation(this IApplicationBuilder app)
    {
        app.UseMiddleware<ImpersonationMiddleware>();
    }

    public class ImpersonationMiddleware : IMiddleware
    {
        private readonly ILogger<ImpersonationMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public ImpersonationMiddleware(ILogger<ImpersonationMiddleware> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (_environment.IsDevelopment())
            {
                var impersonateAs = context.Request.Headers["x-impersonate-as"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(impersonateAs))
                {
                    var newIdentity = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, impersonateAs),
                        new Claim(ClaimTypes.Name, impersonateAs)
                    });

                    _logger.LogDebug("User {UserId} is impersonating {ImpersonatedUserId}", context.User.Identity?.Name, impersonateAs);

                    context.User = new ClaimsPrincipal(newIdentity);
                }
            }

            await next(context);
        }
    }
}
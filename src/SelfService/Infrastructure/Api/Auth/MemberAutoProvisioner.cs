using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Auth;

public class MemberAutoProvisioner : IMiddleware
{
    public const string CallerItemKey = "caller";

    private readonly IMemberRepository _memberRepository;
    private readonly IMemberApplicationService _memberApplicationService;
    private readonly ILogger<MemberAutoProvisioner> _logger;

    public MemberAutoProvisioner(
        IMemberRepository memberRepository,
        IMemberApplicationService memberApplicationService,
        ILogger<MemberAutoProvisioner> logger
    )
    {
        _memberRepository = memberRepository;
        _memberApplicationService = memberApplicationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var caller = context.User?.TryGetCallerIdentity();
        if (caller != null)
        {
            context.Items[CallerItemKey] = caller;

            if (caller.Type == MemberType.ServicePrincipal)
            {
                try
                {
                    var existing = await _memberRepository.FindBy(caller.Id);
                    if (existing == null)
                    {
                        await _memberApplicationService.RegisterServicePrincipal(
                            caller.Id,
                            caller.Email,
                            caller.DisplayName
                        );
                    }
                    else if (existing.DisplayName != caller.DisplayName)
                    {
                        await _memberApplicationService.SyncServicePrincipalDisplayName(caller.Id, caller.DisplayName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-provision service principal {UserId}", caller.Id);
                }
            }
        }

        await next(context);
    }
}

public static class MemberAutoProvisionerExtensions
{
    public static IApplicationBuilder UseMemberAutoProvisioner(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MemberAutoProvisioner>();
    }
}

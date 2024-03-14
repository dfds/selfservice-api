using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Me;

[Route("portalvisits")]
[ApiController]
public class PortalVisitController : ControllerBase
{
    private readonly IPortalVisitApplicationService _portalVisitApplicationService;

    public PortalVisitController(IPortalVisitApplicationService portalVisitApplicationService)
    {
        _portalVisitApplicationService = portalVisitApplicationService;
    }

    [HttpPost("")]
    [UserActionNoAudit]
    public async Task<IActionResult> RegisterVisit()
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Access Denied!",
                    Detail = $"Value \"{User.Identity?.Name}\" is not a valid user id."
                }
            );
        }

        await _portalVisitApplicationService.RegisterVisit(userId);

        return Accepted();
    }
}

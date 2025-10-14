using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Demos;

[Route("demos")]
[Produces("application/json")]
[ApiController]
public class DemoController : ControllerBase
{
    private readonly IDemoApplicationService _demoApplicationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ApiResourceFactory _apiResourceFactory;

    public DemoController(
        IDemoApplicationService demoApplicationService,
        IAuthorizationService authorizationService,
        ApiResourceFactory apiResourceFactory
    )
    {
        _demoApplicationService = demoApplicationService;
        _authorizationService = authorizationService;
        _apiResourceFactory = apiResourceFactory;
    }

    [HttpGet("signups")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> GetActiveSignups()
    {
        if (!User.TryGetUserId(out var principalId))
        {
            return Unauthorized(
                new ProblemDetails { Title = "Unauthorized", Detail = "You are not authorized to perform this action" }
            );
        }

        var isCloudEngineer = _authorizationService.CanSynchronizeAwsECRAndDatabaseECR(User.ToPortalUser());
        if (!isCloudEngineer)
            return Unauthorized();

        return Ok(_apiResourceFactory.Convert(await _demoApplicationService.GetActiveSignups()));
    }
}

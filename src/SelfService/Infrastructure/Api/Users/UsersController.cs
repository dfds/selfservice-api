using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Queries;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Users;

[Route("users")]
[Produces("application/json")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserEmailQuery _userEmailQuery;
    private readonly IAuthorizationService _authorizationService;

    public UsersController(IUserEmailQuery userEmailQuery, IAuthorizationService authorizationService)
    {
        _userEmailQuery = userEmailQuery;
        _authorizationService = authorizationService;
    }

    [HttpGet("emails")]
    [ProducesResponseType(typeof(IEnumerable<UserEmailApiResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetUserEmails(
        [FromQuery(Name = "role")] string[]? roles,
        [FromQuery(Name = "cost-centre")] string[]? costCentres,
        [FromQuery(Name = "business-capability")] string[]? businessCapabilities,
        [FromQuery(Name = "capability")] string[]? capabilities
    )
    {
        try
        {
            // Check if user is a cloud engineer
            var isCloudEngineer = _authorizationService.CanGetUserEmails(User.ToPortalUser());
            if (!isCloudEngineer)
            {
                return Unauthorized(
                    new ProblemDetails
                    {
                        Title = "Unauthorized",
                        Detail = "Only cloud engineers can access user emails.",
                    }
                );
            }

            var users = await _userEmailQuery.GetUsersWithFilters(
                roles,
                costCentres,
                businessCapabilities,
                capabilities
            );

            var response = users.Select(u => new UserEmailApiResource { Name = u.Name, Email = u.Email }).ToList();

            return Ok(response);
        }
        catch (Exception e)
        {
            return CustomObjectResult.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetUserEmails: {e.Message}." }
            );
        }
    }
}

public class UserEmailApiResource
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

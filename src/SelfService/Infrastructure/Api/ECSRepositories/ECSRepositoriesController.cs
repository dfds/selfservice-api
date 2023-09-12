using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.ECSRepositories;

[Route("ecs")]
[Produces("application/json")]
[ApiController]
public class ECSRepositoriesController : ControllerBase
{
    private readonly IECRRepositoryService _ecrRepositoryService;

    public ECSRepositoriesController(IECRRepositoryService ecrRepositoryService)
    {
        _ecrRepositoryService = ecrRepositoryService;
    }

    [HttpGet("repositories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetECSRepositories()
    {
        try
        {
            var repositories = await _ecrRepositoryService.GetAllECRRepositories();
            return Ok(repositories);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetECSRepositories: {e.Message}." }
            );
        }
    }

    private bool IsValidRequest(NewECSRepositoryRequest request)
    {
        return !string.IsNullOrEmpty(request.RepositoryName)
            && !string.IsNullOrEmpty(request.Name)
            && !string.IsNullOrEmpty(request.Description);
    }

    [HttpPost("repositories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> CreateECSRepository([FromBody] NewECSRepositoryRequest request)
    {
        try
        {
            if (!User.TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            if (!IsValidRequest(request))
            {
                return BadRequest();
            }

            // Safe to suppress null
            var name = request.Name!;
            var description = request.Description!;
            var repositoryName = request.RepositoryName!;

            var newRepo = await _ecrRepositoryService.AddRepository(name, description, repositoryName, userId);
            return Ok(newRepo);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"CreateECSRepository: {e.Message}." }
            );
        }
    }
}

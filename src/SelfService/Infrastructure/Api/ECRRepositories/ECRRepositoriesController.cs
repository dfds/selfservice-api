using Microsoft.AspNetCore.Mvc;
using SelfService.Domain.Services;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.ECRRepositories;

[Route("ecr")]
[Produces("application/json")]
[ApiController]
public class ECRRepositoriesController : ControllerBase
{
    private readonly IECRRepositoryService _ecrRepositoryService;
    private readonly IAuthorizationService _authorizationService;

    public ECRRepositoriesController(
        IECRRepositoryService ecrRepositoryService,
        IAuthorizationService authorizationService
    )
    {
        _ecrRepositoryService = ecrRepositoryService;
        _authorizationService = authorizationService;
    }

    [HttpGet("repositories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> GetECRRepositories()
    {
        try
        {
            var repositories = await _ecrRepositoryService.GetAllECRRepositories();
            return Ok(repositories);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetECRRepositories: {e.Message}." }
            );
        }
    }

    private bool IsValidRequest(NewECRRepositoryRequest request)
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
    public async Task<IActionResult> CreateECRRepository([FromBody] NewECRRepositoryRequest request)
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

            bool hasRepository = await _ecrRepositoryService.HasRepository(repositoryName);

            if (hasRepository)
            {
                return BadRequest(
                    new ProblemDetails()
                    {
                        Title = "Repository already exists",
                        Detail = $"Repository with name {repositoryName} already exists",
                    }
                );
            }

            var newRepo = await _ecrRepositoryService.AddRepository(name, description, repositoryName, userId);
            return Ok(newRepo);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails
                {
                    Title = "Uncaught Exception",
                    Detail = $"CreateECRRepository: {e.InnerException}."
                }
            );
        }
    }

    [HttpPost("synchronize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> SynchronizeAwsAndDatabase([FromQuery] bool updateOnMismatch)
    {
        try
        {
            // user wish to potentially delete/add records to the database: lets check credentials
            if (updateOnMismatch)
            {
                var isCloudEngineer = _authorizationService.CanSynchronizeAwsECRAndDatabaseECR(User.ToPortalUser());
                if (!isCloudEngineer)
                    return Unauthorized();
            }

            await _ecrRepositoryService.SynchronizeAwsECRAndDatabase(updateOnMismatch);
            return Ok();
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"CreateECRRepository: {e.Message}." }
            );
        }
    }
}

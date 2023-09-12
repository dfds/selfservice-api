using Amazon.ECR.Model;
using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;
using SelfService.Infrastructure.Api.ECSRepositories;

namespace SelfService.Infrastructure.Api.Metrics;

[Route("ecs")]
[Produces("application/json")]
[ApiController]
public class ECSRepositoriesController : ControllerBase
{
    private readonly IAwsECRRepositoryApplicationService _awsEcrRepositoryApplicationService;
    private readonly IECRRepositoryService _ecrRepositoryService;

    public ECSRepositoriesController(
        IAwsECRRepositoryApplicationService awsEcrRepositoryApplicationService,
        IECRRepositoryService ecrRepositoryService
    )
    {
        _awsEcrRepositoryApplicationService = awsEcrRepositoryApplicationService;
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

            await _awsEcrRepositoryApplicationService.CreateECRRepo(name);
            try
            {
                var newRepo = new ECRRepository(new ECRRepositoryId(), name, description, repositoryName, userId);
                _ecrRepositoryService.AddRepository(newRepo);
                return Ok(newRepo);
            }
            catch (Exception e)
            {
                await _awsEcrRepositoryApplicationService.DeleteECRRepo(repositoryName);
                throw new Exception($"Error creating repo {repositoryName}: {e.Message}");
            }
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"CreateECSRepository: {e.Message}." }
            );
        }
    }
}

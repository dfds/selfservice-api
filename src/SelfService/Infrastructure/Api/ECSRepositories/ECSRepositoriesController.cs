using Amazon.ECR.Model;
using Microsoft.AspNetCore.Mvc;
using SelfService.Application;
using SelfService.Domain.Exceptions;
using SelfService.Domain.Models;
using SelfService.Infrastructure.Api.Capabilities;

namespace SelfService.Infrastructure.Api.Metrics;

[Route("ecs")]
[Produces("application/json")]
[ApiController]
public class ECSRepositoriesController : ControllerBase
{
    private readonly IAwsECRRepoApplicationService _awsECRRepoApplicationService;
    private readonly IECRRepositoryRepository _ecrRepositoryRepository;

    public ECSRepositoriesController(
        IAwsECRRepoApplicationService awsECRRepoApplicationService,
        IECRRepositoryRepository ecrRepositoryRepository
    )
    {
        _awsECRRepoApplicationService = awsECRRepoApplicationService;
        _ecrRepositoryRepository = ecrRepositoryRepository;
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
            var repositories = await _ecrRepositoryRepository.GetAll();

            return Ok(repositories);
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetCapabilityCosts: {e.Message}." }
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

            await _awsECRRepoApplicationService.CreateECRRepo(name);
            try
            {
                var newRepo = _ecrRepositoryRepository.AddRepository(
                    new ECSRepository(Guid.NewGuid(), name, description, repositoryName, userId)
                );
                return Ok(newRepo);
            }
            catch (Exception e)
            {
                await _awsECRRepoApplicationService.DeleteECRRepo(repositoryName);
                throw new Exception($"Error creating repo {repositoryName}: {e.Message}");
            }
        }
        catch (PlatformDataApiUnavailableException e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails
                {
                    Title = "PlatformDataApi unreachable",
                    Detail = $"PlatformDataApi error: {e.Message}."
                }
            );
        }
        catch (Exception e)
        {
            return CustomObjectResults.InternalServerError(
                new ProblemDetails { Title = "Uncaught Exception", Detail = $"GetCapabilityCosts: {e.Message}." }
            );
        }
    }
}
